using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

// How much a machine holds or uses. Every patch runs inside the game's own call, so nothing may escape it.

/// <summary>
/// A brewing barrel's bottle limit is a game setting read through one lookup, by the brew preview, the panel and the
/// brew start alike, so the limit is multiplied there.
/// </summary>
internal static class BarrelCapacityPatch
{
    private const string MaxBottleKey = "Brew_MaxBottle";

    private static Multiplier _capacity = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing the barrel capacity", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "barrel capacity", () =>
            harmony.Patch(Machine.Method(typeof(Furniture), "GetBrewSetting"), postfix: Machine.Patch(typeof(BarrelCapacityPatch), nameof(ScaleLimit))));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier capacity)
    {
        _capacity = capacity;
        _hook?.Use(capacity);
    }

    private static void ScaleLimit(string key, ref float __result)
    {
        try
        {
            if (key != MaxBottleKey) return;

            var gameLimit = __result;

            var limit = Amounts.Capacity(gameLimit, _capacity);

            __result = limit;
            if (_first.IsPending) _first.Note($"first barrel limit: game {gameLimit:0.##} bottles, now {limit:0.##}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// A robot's charging rate is worked out from its base rate and modules in two places, when it is set up and when its
/// modules change, and every charge reads the result. The result is multiplied there, and each robot seen is kept so a
/// change of the setting moves its rate over at once.
/// </summary>
internal static class RobotChargingPatch
{
    private static readonly Dictionary<IntPtr, (RobotComponent Robot, float Applied)> Robots = new();

    private static Multiplier _charging = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing robot charging", "rate"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "robot charging", () =>
        {
            harmony.Patch(Machine.Method(typeof(RobotComponent), "ResetNumericModuleState"), postfix: Machine.Patch(typeof(RobotChargingPatch), nameof(ScaleCharging)));
            harmony.Patch(Machine.Method(typeof(RobotComponent), "RecalcNumericModules"), postfix: Machine.Patch(typeof(RobotChargingPatch), nameof(ScaleCharging)));
        });
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier charging)
    {
        _charging = charging;
        _hook?.Use(charging);

        if (_hook?.IsIn == true) MoveRobotsOver();
    }

    private static void ScaleCharging(RobotComponent __instance)
    {
        try
        {
            if (__instance == null) return;

            var gameRate = __instance._chargeTripsPerHourCfg;

            var rate = Amounts.Rate(gameRate, _charging);

            __instance._chargeTripsPerHourCfg = rate;
            Robots[__instance.Pointer] = (__instance, _charging.Value);
            if (_first.IsPending) _first.Note($"first robot charging rate: game {gameRate:0.###}, now {rate:0.###}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void MoveRobotsOver()
    {
        foreach (var pointer in Robots.Keys.ToList())
        {
            try
            {
                var (robot, applied) = Robots[pointer];

                robot._chargeTripsPerHourCfg = Amounts.Reapply(robot._chargeTripsPerHourCfg, applied, _charging);
                Robots[pointer] = (robot, _charging.Value);
            }
            catch (Exception ex)
            {
                Robots.Remove(pointer);
                _failures.Report(ex);
            }
        }
    }
}

/// <summary>
/// How fast food spoils in a powered fridge or freezer is the fridge's cold rate from the furniture table, read when an
/// item's spoil rate is worked out. The rate is divided for the length of that one call and put back after it, so
/// everything else that reads the table (which storage counts as a fridge, the freezer labels) sees the game's value.
/// The game's own rules still apply: the fridge is only used while powered, and a cold room colder than the fridge
/// wins. The panels' preservation figures are brought in step.
/// The fridge talent is the exception: the game applies it after reading the rate and never lets it go under a tenth,
/// which would undo the strength. So the talent goes into the rate here, and the game's own talent read for that call
/// answers 0.
/// </summary>
internal static class FridgeStrengthPatch
{
    private const int FridgeElectricalType = 1;
    private const string FridgeTalentKey = "Buff_AE_FridgeColdEnhance";

    private static Multiplier _strength = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;
    private static readonly StampedCache<int, long, Config_Furniture?> FridgesThisFrame = new();
    private static Config_Furniture? _changedRow;
    private static float _gameRate;
    private static bool _hideTalent;
    private static int _talentFrame = -1;
    private static float _talent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing fridge strength", "rate"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "fridge strength",
            () =>
            {
                harmony.Patch(Machine.Method(typeof(ItemManager), "GetTalentEffectRatio"), prefix: Machine.Patch(typeof(FridgeStrengthPatch), nameof(HideTalent)));
                harmony.Patch(Machine.Method(typeof(ItemManager), "ComputeItemTimeScale"), prefix: Machine.Patch(typeof(FridgeStrengthPatch), nameof(StrengthenFridge)),
                    postfix: Machine.Patch(typeof(FridgeStrengthPatch), nameof(PutBack)));
            },
            () =>
            {
                harmony.Patch(Machine.Method(typeof(FridgeDisplayRules), nameof(FridgeDisplayRules.GetEffectiveColdRate)), postfix: Machine.Patch(typeof(FridgeStrengthPatch), nameof(ScaleShownRate)));
                harmony.Patch(Machine.Method(typeof(FridgeDisplayRules), nameof(FridgeDisplayRules.GetPreservationRate)), postfix: Machine.Patch(typeof(FridgeStrengthPatch), nameof(ScaleShownFigure)));
            });
    }

    /// <summary>Called on the main thread at start and after each change of the setting. Food already stored follows as the game refreshes it.</summary>
    public static void Use(Multiplier strength)
    {
        _strength = strength;
        _hook?.Use(strength);
    }

    private static void StrengthenFridge(ItemData item)
    {
        try
        {
            PutBack();

            if (item == null || _strength.IsGameAmount) return;

            var frame = UnityEngine.Time.frameCount;

            if (FridgeRowOf(item, frame) is not { } row) return;

            var gameRate = row._ColdRate_k__BackingField;
            var talent = FridgeTalent(frame);
            var rate = Amounts.FridgeRate(gameRate, talent, _strength);

            if (rate == Amounts.TalentColdRate(gameRate, talent)) return;

            (_changedRow, _gameRate, _hideTalent) = (row, gameRate, talent > 0f);
            row._ColdRate_k__BackingField = rate;
            if (_first.IsPending) _first.Note($"first fridge cold rate: game {gameRate:0.###}, talent {talent:0.###}, now {rate:0.###}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void PutBack()
    {
        _hideTalent = false;

        var row = _changedRow;

        if (row == null) return;

        _changedRow = null;
        row._ColdRate_k__BackingField = _gameRate;
    }

    /// <summary>
    /// The table row of the powered fridge or freezer holding the item, or null when it is held by anything else. The
    /// game works out many items' spoil rates in one frame, so each holder is looked up once in that frame.
    /// </summary>
    private static Config_Furniture? FridgeRowOf(ItemData item, int frame)
    {
        var owner = item._OwnerId_k__BackingField;

        FridgesThisFrame.Use(frame);

        if (FridgesThisFrame.TryGet(owner, out var known)) return known;

        var holder = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetAgent(owner);
        Config_Furniture? fridge = null;

        if (holder != null && holder._InstanceIdType_k__BackingField == InstanceIdType.Furniture)
        {
            var row = GameSingleton.Existing<ConfigManager>()?.Get_Config_Furniture(holder._AgentConfigId_k__BackingField);

            if (row != null && row._IsElectrical_k__BackingField && row._ElectricalType_k__BackingField == FridgeElectricalType) fridge = row;
        }

        FridgesThisFrame.Set(owner, fridge);

        return fridge;
    }

    /// <summary>The fridge talent's share, read from the game once a frame while no talent read is being hidden.</summary>
    private static float FridgeTalent(int frame)
    {
        if (frame != _talentFrame)
        {
            _talent = ItemManager.GetTalentEffectRatio(FridgeTalentKey);
            _talentFrame = frame;
        }

        return _talent;
    }

    /// <summary>The game's talent read inside a changed fridge's spoil rate call answers 0: the talent is in the rate already.</summary>
    private static bool HideTalent(string effectConfigName, ref float __result)
    {
        if (!_hideTalent || effectConfigName != FridgeTalentKey) return true;

        _hideTalent = false;
        __result = 0f;

        return false;
    }

    /// <summary>The panels' fridge rate has the talent in it already, so the strength goes on after it.</summary>
    private static void ScaleShownRate(ref float __result)
    {
        try
        {
            __result = Amounts.ColdRate(__result, _strength);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    /// <summary>The panels' preservation figure, worked out again from the strengthened rate.</summary>
    private static void ScaleShownFigure(float coldRate, ref int __result)
    {
        try
        {
            if (_strength.IsGameAmount) return;

            __result = FoodPreservationRules.GetDisplayPreservationRate(FridgeDisplayRules.GetEffectiveColdRate(coldRate));
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// Defense devices lose durability through one call, for wear and attacks alike, with the loss as a negative number.
/// The loss is multiplied there for devices only; doors, windows and repairs are left alone. The wave report adds up
/// the game's loss in another place, so it keeps counting the game's amount.
/// </summary>
internal static class EquipmentWearPatch
{
    private const int DefenseSlot = 10;

    private static readonly RemainderCarry<long> Carry = new();
    private static readonly StampedCache<int, long, bool> DevicesThisFrame = new();

    private static Multiplier _wear = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing equipment wear", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "equipment wear", () =>
            harmony.Patch(Machine.Method(typeof(FurnitureDurabilityComponent), nameof(FurnitureDurabilityComponent.TakeDamage)), prefix: Machine.Patch(typeof(EquipmentWearPatch), nameof(ScaleWear))));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier wear)
    {
        _wear = wear;
        _hook?.Use(wear);
    }

    private static void ScaleWear(FurnitureDurabilityComponent __instance, ref int damage, bool isRepair)
    {
        try
        {
            if (__instance == null || isRepair || damage >= 0 || _wear.IsGameAmount) return;

            var device = __instance._ownerInstanceId_k__BackingField;

            if (!IsDefenseDevice(device)) return;

            var gameLoss = -damage;
            var loss = Carry.Take(device, gameLoss, _wear);

            damage = -loss;
            if (_first.IsPending) _first.Note($"first equipment wear: game {gameLoss}, now {loss}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    /// <summary>A wave can wear many pieces many times in one frame, so each piece is looked up once in that frame.</summary>
    private static bool IsDefenseDevice(long furnitureId)
    {
        DevicesThisFrame.Use(UnityEngine.Time.frameCount);

        if (DevicesThisFrame.TryGet(furnitureId, out var known)) return known;

        var furniture = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetAgent(furnitureId);
        var isDevice = furniture != null && furniture._InstanceIdType_k__BackingField == InstanceIdType.Furniture &&
                       GameSingleton.Existing<ConfigManager>()?.Get_Config_Furniture(furniture._AgentConfigId_k__BackingField)?.SlotType == DefenseSlot;

        DevicesThisFrame.Set(furnitureId, isDevice);

        return isDevice;
    }
}
