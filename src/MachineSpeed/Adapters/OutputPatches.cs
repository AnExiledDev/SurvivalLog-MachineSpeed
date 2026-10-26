using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;
using Il2CppCounts = Il2CppSystem.Collections.Generic.Dictionary<int, int>;
using Il2CppYields = Il2CppSystem.Collections.Generic.List<GameCore.HotUpdate.ShredderYieldInfo>;

namespace MachineSpeed.Adapters;

// How much a machine makes. Each class keeps its own multiplier and its own carried fractions, so 1.5 on single items
// gives 1, 2, 1, 2 for each kind of item. Every patch runs inside the game's own call, so nothing may escape it.

/// <summary>
/// The shredder's yields for an item are worked out fresh on every call, for the panel, the fit check and the payout
/// alike. Each call is shown the multiplied counts, so the fit check counts the extra items; the carried fractions are
/// only used up when a shred is paid out, which is when the shredded item is gone afterwards.
/// </summary>
internal static class ShredderOutputPatch
{
    private static readonly RemainderCarry<int> Carry = new();
    private static readonly Dictionary<int, int> PaidOut = new();

    private static Multiplier _output = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;
    private static bool _isSettling;
    private static long _shreddedItemId;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing the shredder's output", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "shredder output", () =>
        {
            harmony.Patch(Machine.Method(typeof(ConfigManager), nameof(ConfigManager.GetShredYields)), postfix: Machine.Patch(typeof(ShredderOutputPatch), nameof(ScaleYields)));
            harmony.Patch(Machine.Method(typeof(Furniture), "SettleShred"), prefix: Machine.Patch(typeof(ShredderOutputPatch), nameof(BeginSettle)),
                postfix: Machine.Patch(typeof(ShredderOutputPatch), nameof(EndSettle)));
        });
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier output)
    {
        _output = output;
        _hook?.Use(output);
    }

    private static void ScaleYields(Il2CppYields __result)
    {
        try
        {
            if (__result == null || _output.IsGameAmount) return;

            for (var i = 0; i < __result.Count; i++)
            {
                var share = __result[i];

                if (share == null) continue;

                var gameCount = share.Count;

                if (_isSettling) PaidOut[share.ConfigId] = gameCount;

                share.Count = Carry.Peek(share.ConfigId, gameCount, _output);
            }
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void BeginSettle(Furniture __instance)
    {
        try
        {
            PaidOut.Clear();
            _shreddedItemId = __instance?._shredderComp?.HeadItemId ?? 0;
            _isSettling = _shreddedItemId != 0;
        }
        catch (Exception ex)
        {
            _isSettling = false;
            _failures.Report(ex);
        }
    }

    /// <summary>A shred that did not fit is tried again later with the same fractions, so only a finished one uses them up.</summary>
    private static void EndSettle()
    {
        try
        {
            var items = _isSettling ? GameSingleton.Existing<BattleLogicWorld>()?._ItemManager : null;
            var isPaidOut = items != null && items.GetItemData(_shreddedItemId) == null;

            if (!isPaidOut) return;

            foreach (var (configId, gameCount) in PaidOut)
            {
                var count = Carry.Take(configId, gameCount, _output);

                if (_first.IsPending) _first.Note($"first shredder payout: item {configId}, game {gameCount}, now {count}");
            }
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
        finally
        {
            _isSettling = false;
            PaidOut.Clear();
        }
    }
}

/// <summary>
/// The Ration Press freezes a batch's block and crumb counts when it starts and pays them out when it finishes. The
/// counts are multiplied as the batch starts; the flavors are spread over the new block count in the same shares.
/// </summary>
internal static class RationPressOutputPatch
{
    private const string Blocks = "blocks";
    private const string Crumbs = "crumbs";

    private static readonly RemainderCarry<string> Carry = new();

    private static Multiplier _output = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing the ration press output", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "ration press output", () =>
            harmony.Patch(Machine.Method(typeof(CompressorComponent), nameof(CompressorComponent.BeginCompress)), prefix: Machine.Patch(typeof(RationPressOutputPatch), nameof(ScaleBatch))));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier output)
    {
        _output = output;
        _hook?.Use(output);
    }

    private static void ScaleBatch(CompressBatch batch)
    {
        try
        {
            if (batch == null || _output.IsGameAmount) return;

            var gameBlocks = batch.Blocks;
            var gameCrumbs = batch.Crumbs;
            var blocks = Carry.Take(Blocks, gameBlocks, _output);
            var crumbs = Carry.Take(Crumbs, gameCrumbs, _output);

            SpreadFlavors(batch, gameBlocks, blocks);
            batch.Blocks = blocks;
            batch.Crumbs = crumbs;
            if (_first.IsPending) _first.Note($"first ration batch output: game {gameBlocks} blocks and {gameCrumbs} crumbs, now {blocks} and {crumbs}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void SpreadFlavors(CompressBatch batch, int gameBlocks, int blocks)
    {
        var flavors = batch.FlavorBlocks;

        if (flavors == null || flavors.Length == 0) return;

        var parts = new int[flavors.Length];

        for (var i = 0; i < parts.Length; i++) parts[i] = flavors[i];

        var spread = Shares.Rescale(parts, gameBlocks, blocks);

        for (var i = 0; i < spread.Length; i++) flavors[i] = spread[i];
    }
}

/// <summary>
/// A brew is handed its bottle count when it starts and pays exactly that out at the end, so the count is multiplied
/// there. The panel's count for a brew not yet started is the game's, so it is brought in step.
/// </summary>
internal static class BrewingOutputPatch
{
    private const int Idle = 0;

    private static readonly RemainderCarry<int> Carry = new();

    private static Multiplier _output = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing the brewing output", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "brewing output",
            () => harmony.Patch(Machine.Method(typeof(BrewComponent), nameof(BrewComponent.BeginBrew)), prefix: Machine.Patch(typeof(BrewingOutputPatch), nameof(ScaleBrew))),
            () => harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildBrewPanelData)), postfix: Machine.Patch(typeof(BrewingOutputPatch), nameof(ScalePanel))));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier output)
    {
        _output = output;
        _hook?.Use(output);
    }

    private static void ScaleBrew(int resultItemId, ref int resultCount)
    {
        try
        {
            var gameCount = resultCount;

            var count = Carry.Take(resultItemId, gameCount, _output);

            resultCount = count;
            if (_first.IsPending) _first.Note($"first brew output: game {gameCount} bottles, now {count}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void ScalePanel(BrewPanelData __result)
    {
        try
        {
            if (__result != null && __result.State == Idle) __result.ResultCount = Carry.Peek(__result.ResultItemId, __result.ResultCount, _output);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// Food that rots in a compost bin is swapped for compost, as many as the rotted stack held. The compost product is
/// read before the game removes the food, and the extra compost goes into the same bin; extra that does not fit is lost.
/// </summary>
internal static class CompostOutputPatch
{
    private static readonly RemainderCarry<int> Carry = new();

    private static Multiplier _output = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;
    private static (long Bin, int Product, int Count)? _pending;
    private static readonly StampedCache<int, long, bool> BinsThisFrame = new();

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing the compost output", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "compost output", () =>
            harmony.Patch(Machine.Method(typeof(ItemManager), "CheckAndProcessRot"), prefix: Machine.Patch(typeof(CompostOutputPatch), nameof(ReadProduct)),
                postfix: Machine.Patch(typeof(CompostOutputPatch), nameof(AddExtra))));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier output)
    {
        _output = output;
        _hook?.Use(output);
    }

    private static void ReadProduct(ItemManager __instance, ItemData item, Config_Item config)
    {
        try
        {
            _pending = null;

            if (__instance == null || item == null || config == null || _output.IsGameAmount) return;

            var bin = item._OwnerId_k__BackingField;

            if (!IsBin(__instance, bin) || !__instance.TryGetCompostTierProduct(bin, item, config, out var product) || product <= 0) return;

            _pending = (bin, product, item._ItemCount_k__BackingField);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    /// <summary>
    /// The game checks every item that can rot at the turn of each hour, all in one frame, so whether a holder is a bin
    /// is asked once per holder in that frame, not once per item.
    /// </summary>
    private static bool IsBin(ItemManager items, long holder)
    {
        BinsThisFrame.Use(UnityEngine.Time.frameCount);

        if (BinsThisFrame.TryGet(holder, out var isBin)) return isBin;

        isBin = items.IsInCompostBin(holder);
        BinsThisFrame.Set(holder, isBin);

        return isBin;
    }

    private static void AddExtra(ItemManager __instance, bool __result, int currentHour)
    {
        try
        {
            if (!__result || _pending is not { } pending || pending.Count <= 0) return;

            var (bin, product, gameCount) = pending;
            var extra = Carry.Take(product, gameCount, _output) - gameCount;
            var productConfig = GameSingleton.Existing<ConfigManager>()?.Get_Config_Item(product);

            if (extra <= 0 || productConfig == null) return;

            var timeLeft = productConfig.Life <= 0 ? -1 : 24 * productConfig.Life;
            var isAdded = __instance.TryAddItemToBag(bin, product, extra, currentHour, timeLeft, productConfig.UseTimes, 1f, null, 0, null, null, 0, 0, false, false, false, false, 0);

            if (_first.IsPending) _first.Note($"first compost output: game {gameCount} of item {product}, {extra} more {(isAdded ? "added" : "did not fit")}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
        finally
        {
            _pending = null;
        }
    }
}

/// <summary>
/// A harvest rolls its crops, perfect crops and seeds into three counts and hands each to the player, or leaves them
/// for a harvesting robot to collect. The crop counts are multiplied just before they are handed over; seeds are not.
/// </summary>
internal static class HarvestOutputPatch
{
    private static readonly RemainderCarry<int> Carry = new();

    private static Multiplier _output = Multiplier.GameAmount;
    private static AmountHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;
    private static PlantHarvestYield? _harvest;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_failures, _first) = (new HookFailures(log.Write, "changing the harvest output", "amount"), new FirstUse(log.WriteFileOnly));
        _hook = new AmountHook(log, "harvest output", () =>
        {
            harmony.Patch(Machine.Method(typeof(AE_HarvestPlant), "RollYield"), prefix: Machine.Patch(typeof(HarvestOutputPatch), nameof(BeginHarvest)),
                postfix: Machine.Patch(typeof(HarvestOutputPatch), nameof(EndHarvest)));
            harmony.Patch(Machine.Method(typeof(AE_HarvestPlant), "DeliverAll"), prefix: Machine.Patch(typeof(HarvestOutputPatch), nameof(ScaleCrops)));
        });
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Multiplier output)
    {
        _output = output;
        _hook?.Use(output);
    }

    private static void BeginHarvest(PlantHarvestYield harvest) => _harvest = harvest;

    private static void EndHarvest() => _harvest = null;

    private static void ScaleCrops(Il2CppCounts items)
    {
        try
        {
            var harvest = _harvest;

            if (harvest == null || items == null || _output.IsGameAmount || IsSeeds(harvest, items)) return;

            var crops = new List<int>(items.Count);

            foreach (var pair in items) crops.Add(pair.Key);

            foreach (var crop in crops)
            {
                var gameCount = items[crop];
                var count = Carry.Take(crop, gameCount, _output);

                items[crop] = count;
                if (_first.IsPending) _first.Note($"first harvest output: item {crop}, game {gameCount}, now {count}");
            }
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static bool IsSeeds(PlantHarvestYield harvest, Il2CppCounts items) => harvest.SeedResults?.Pointer == items.Pointer;
}
