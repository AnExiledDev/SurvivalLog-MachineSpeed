using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>One machine found again just before it is rushed, with the clock it was read at.</summary>
internal sealed record FoundMachine(BattleLogicWorld World, Furniture Furniture, MachineRow Row, int NowSeconds);

/// <summary>
/// Reads every machine at home the way the game's own HUD finds furniture: placed, at home, on an unlocked floor and in
/// an unlocked area. Times are read from the machines' own fields, which Machine Speed's patches already keep at the
/// machine's speed. A machine that fails to read is left out, and each distinct failure is logged once.
/// </summary>
internal sealed class MachineReader
{
    private const int SecondsPerHour = 3600;
    private const int ShredderRunning = 1;
    private const int ShredderPaused = 2;
    private const int ShredderDone = 3;

    private readonly Action<string> _log;
    private readonly DistinctFailures _failures = new();

    public MachineReader(Action<string> log)
    {
        _log = log;
    }

    /// <summary>What is at home now, and the game clock in seconds (0 with no game).</summary>
    public (MachineRead Read, long NowSeconds) Read()
    {
        if (LoadedGame() is not { } game) return (new MachineRead.NoGame(), 0);

        var (world, config, agents) = game;

        if (!IsHome(agents)) return (new MachineRead.Away(), 0);

        var now = world._GameTimeManager.GetTotalSeconds();
        var rows = new List<MachineRow>();
        var plants = new PlantTally();
        var furnitures = agents.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture.IsDead) continue;

            try
            {
                if (RowOf(world, config, agents, furniture, now, plants) is { } row) rows.Add(row);
            }
            catch (Exception ex)
            {
                ReportOnce("machine read", ex);
            }
        }

        if (plants.Row() is { } plantRow) rows.Add(plantRow);

        return (new MachineRead.Home(rows, Points(world)), now);
    }

    /// <summary>The machine as it is right now, or null when it is gone, away from home or no longer that kind.</summary>
    public FoundMachine? Find(MachineKind kind, long id)
    {
        if (LoadedGame() is not { } game) return null;

        var (world, config, agents) = game;

        if (!IsHome(agents)) return null;

        var now = world._GameTimeManager.GetTotalSeconds();
        var furnitures = agents.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture.IsDead || furniture.InstanceId != id) continue;

            var row = RowOf(world, config, agents, furniture, now, new PlantTally());

            return row != null && row.Kind == kind ? new FoundMachine(world, furniture, row, now) : null;
        }

        return null;
    }

    /// <summary>The player's survival points; null when they cannot be read.</summary>
    public int? Points(BattleLogicWorld world)
    {
        try
        {
            return world._SurvivalResultsManager?.GetSurvivalPoint();
        }
        catch (Exception ex)
        {
            ReportOnce("survival points read", ex);

            return null;
        }
    }

    private static (BattleLogicWorld World, ConfigManager Config, AgentManager Agents)? LoadedGame()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var agents = world?._AgentManager;
        var role = agents?.GetLeadingRole();

        if (world == null || config == null || agents == null || role == null || world._GameTimeManager?.WorldStateType != WorldStateType.PostDisaster) return null;

        return (world, config, agents);
    }

    private bool IsHome(AgentManager agents)
    {
        try
        {
            return agents.IsHomeMap(ExploreSearchStateManager.GetCurrentMapId());
        }
        catch (Exception ex)
        {
            ReportOnce("home check", ex);

            return false;
        }
    }

    /// <summary>A drone out on a trip is hidden, so only it skips the shown check.</summary>
    private static bool IsAtHome(BattleLogicWorld world, AgentManager agents, Furniture furniture, bool isDrone) =>
        (isDrone || furniture.IsShow) && agents.IsHomeMap(furniture.MapConfigId) && FloorTagConstants.IsMapPointUnlocked(world._TagManager, furniture.MapConfigId) &&
        AreaUnlockConstants.IsFurnitureAreaUnlocked(furniture);

    /// <summary>The window's row for one piece of furniture, or null when it is not a listed machine. Plants go into the tally.</summary>
    private static MachineRow? RowOf(BattleLogicWorld world, ConfigManager config, AgentManager agents, Furniture furniture, int now, PlantTally plants)
    {
        var drone = AgentTools.GetAgentComponent<DroneTripComponent>(furniture);

        if (!IsAtHome(world, agents, furniture, isDrone: drone != null)) return null;

        if (drone != null) return DroneRow(world, config, furniture, drone, now);

        if (CompostConfig.IsCompostBin(furniture.AgentConfigId)) return CompostRow(world, config, furniture);

        if (AgentTools.GetAgentComponent<ShredderComponent>(furniture) is { } shredder)
        {
            var product = shredder.State == 0 ? null : ItemName(config, shredder.HeadConfigId);

            return TimedRow(MachineKind.Shredder, config, furniture, shredder.State, shredder.GetRemainSeconds(now), shredder.TotalSeconds, product);
        }

        if (AgentTools.GetAgentComponent<CompressorComponent>(furniture) is { } press) return TimedRow(MachineKind.RationPress, config, furniture, press.State, press.GetRemainSeconds(now), press.TotalSeconds, null);

        if (AgentTools.GetAgentComponent<DyeComponent>(furniture) is { } dye) return DyeRow(config, furniture, dye, now);

        if (AgentTools.GetAgentComponent<BrewComponent>(furniture) is { } brew) return BrewRow(config, furniture, brew, now);

        if (AgentTools.GetAgentComponent<CookingComponent>(furniture) is { } cooking) return CookingRow(config, furniture, cooking, now);

        if (AgentTools.GetAgentComponent<GeneratorFuelComponent>(furniture) is { IsFuelHeater: false } generator) return GeneratorRow(world, config, furniture, generator);

        if (AgentTools.GetAgentComponent<PlantComponent>(furniture) is { } plant) plants.Add(plant);

        return null;
    }

    /// <summary>
    /// The shredder and the Ration Press share their states: 1 running, 2 paused (power off), 3 done. A running batch's
    /// TotalSeconds is already at the machine's speed.
    /// </summary>
    private static MachineRow TimedRow(MachineKind kind, ConfigManager config, Furniture furniture, int state, int remain, int total, string? product)
    {
        var (shown, isTimed) = state switch
        {
            ShredderRunning => (MachineState.Running, true),
            ShredderPaused => (MachineState.Paused, true),
            ShredderDone => (MachineState.Finished, false),
            _ => (MachineState.Idle, false),
        };

        return Row(kind, config, furniture, product, shown, isTimed ? remain : null, canRush: shown == MachineState.Running, isTimed ? total : null);
    }

    private static MachineRow BrewRow(ConfigManager config, Furniture furniture, BrewComponent brew, int now)
    {
        if (brew.IsBrewing())
        {
            var nowHour = now / SecondsPerHour;
            var done = Countdown.Done(nowHour - brew.StartHour, now, nowHour, HourJobs.Carry("brew", furniture.InstanceId));
            var remain = Countdown.Remaining(brew.TotalHours, done, BrewingPatch.Speed);
            var total = Countdown.Remaining(brew.TotalHours, 0, BrewingPatch.Speed);

            return Row(MachineKind.Brewing, config, furniture, NonEmpty(Furniture.GetBrewProductName(brew.ResultItemId)), MachineState.Running, remain, canRush: true, total);
        }

        return brew.IsDone()
            ? Row(MachineKind.Brewing, config, furniture, NonEmpty(Furniture.GetBrewProductName(brew.ResultItemId)), MachineState.Finished, null, canRush: false)
            : Row(MachineKind.Brewing, config, furniture, null, MachineState.Idle, null, canRush: false);
    }

    private static MachineRow DyeRow(ConfigManager config, Furniture furniture, DyeComponent dye, int now)
    {
        if (dye.IsRunning())
        {
            var nowHour = now / SecondsPerHour;
            var done = Countdown.Done(nowHour - dye.StartHour, now, nowHour, HourJobs.Carry("dye", furniture.InstanceId));
            var remain = Countdown.Remaining(dye.ResultCount * dye.HoursPerBucket, done, DyeVatPatch.Speed);
            var total = Countdown.Remaining(dye.ResultCount * dye.HoursPerBucket, 0, DyeVatPatch.Speed);

            return Row(MachineKind.DyeVat, config, furniture, NonEmpty(Furniture.GetDyeProductName(dye.ResultItemId)), MachineState.Running, remain, canRush: true, total);
        }

        return dye.IsDone()
            ? Row(MachineKind.DyeVat, config, furniture, NonEmpty(Furniture.GetDyeProductName(dye.ResultItemId)), MachineState.Finished, null, canRush: false)
            : Row(MachineKind.DyeVat, config, furniture, null, MachineState.Idle, null, canRush: false);
    }

    /// <summary>A dish waiting to be taken out leaves the stove idle with a pending product.</summary>
    private static MachineRow CookingRow(ConfigManager config, Furniture furniture, CookingComponent cooking, int now)
    {
        var start = cooking.CookStartTotalSeconds;

        return cooking.State switch
        {
            CookingState.Running => Row(MachineKind.Cooking, config, furniture, null, MachineState.Running, Math.Max(0, cooking.CookDuration - (now - start)), canRush: true, cooking.CookDuration),
            CookingState.Paused => Row(MachineKind.Cooking, config, furniture, null, MachineState.Paused, Math.Max(0, cooking.CookDuration - (cooking.PauseStartTotalSeconds - start)), canRush: false, cooking.CookDuration),
            _ when cooking.HasPendingProduct => Row(MachineKind.Cooking, config, furniture, NonEmpty(cooking.PendingProductName), MachineState.Finished, null, canRush: false),
            _ => Row(MachineKind.Cooking, config, furniture, null, MachineState.Idle, null, canRush: false),
        };
    }

    /// <summary>
    /// A drone still out is running until its return time; one back with cargo not yet unloaded is finished. While a
    /// trader's visit holds the trade screen, a trip is not cut short, so the game's trade is never pulled from under it.
    /// </summary>
    private static MachineRow DroneRow(BattleLogicWorld world, ConfigManager config, Furniture furniture, DroneTripComponent drone, int now)
    {
        var isBusy = drone.IsBusy();

        if (isBusy && drone.ReturnSec > now)
        {
            var isTradeLocked = world._StrangerTradeActiveManager?._IsLocked_k__BackingField != false;

            return Row(MachineKind.Drone, config, furniture, null, MachineState.Running, drone.ReturnSec - now, canRush: !isTradeLocked, drone.ReturnSec - drone.DepartSec);
        }

        var hasCargo = isBusy || StrangerTradeActiveManager.HasCargoLeftover(furniture.InstanceId);

        return Row(MachineKind.Drone, config, furniture, null, hasCargo ? MachineState.Finished : MachineState.Idle, null, canRush: false);
    }

    /// <summary>A generator that is switched off is shown as paused: its fuel is not burning.</summary>
    private static MachineRow GeneratorRow(BattleLogicWorld world, ConfigManager config, Furniture furniture, GeneratorFuelComponent generator)
    {
        if (!generator.HasFuel()) return Row(MachineKind.Generator, config, furniture, null, MachineState.OutOfFuel, null, canRush: false);

        var isOn = world._PowerManager?.GetGeneratorState(furniture.InstanceId) == true;

        return Row(MachineKind.Generator, config, furniture, null, isOn ? MachineState.Running : MachineState.Paused, generator.GetRemainingGameSeconds(), canRush: false);
    }

    /// <summary>A bin is working while it holds anything; it has no single finish time.</summary>
    private static MachineRow CompostRow(BattleLogicWorld world, ConfigManager config, Furniture furniture)
    {
        var items = world._ItemManager?.GetItemDataList(furniture.InstanceId);
        var count = 0;

        for (var i = 0; items != null && i < items.Count; i++)
        {
            var item = items[i];

            if (item != null) count += item.ItemCount <= 0 ? 1 : item.ItemCount;
        }

        return count > 0
            ? new MachineRow(MachineKind.Compost, furniture.InstanceId, FurnitureName(config, furniture), new Contents.Items(count), MachineState.Running, null, false, Icon: Icon(MachineKind.Compost, config, furniture))
            : new MachineRow(MachineKind.Compost, furniture.InstanceId, FurnitureName(config, furniture), null, MachineState.Idle, null, false, Icon: Icon(MachineKind.Compost, config, furniture));
    }

    private static MachineRow Row(MachineKind kind, ConfigManager config, Furniture furniture, string? product, MachineState state, double? remain, bool canRush, double? total = null) =>
        new(kind, furniture.InstanceId, FurnitureName(config, furniture), product == null ? null : new Contents.Named(product), state, remain, canRush, total, Icon(kind, config, furniture));

    private static string Icon(MachineKind kind, ConfigManager config, Furniture furniture) => MachineIcons.Of(kind, config.Get_Config_Furniture(furniture.AgentConfigId)?.ICON);

    private static string FurnitureName(ConfigManager config, Furniture furniture)
    {
        var row = config.Get_Config_Furniture(furniture.AgentConfigId);

        return row == null ? "" : Translate(config, row.Name) ?? NonEmpty(row.Name_Local) ?? NonEmpty(row.Name) ?? "";
    }

    private static string? ItemName(ConfigManager config, int itemId)
    {
        var item = config.Get_Config_Item(itemId);

        return item == null ? null : Translate(config, item.ItemName) ?? NonEmpty(item.ItemName_Local) ?? NonEmpty(item.ItemName);
    }

    /// <summary>The game's text for a key; null when there is none, which the game shows as the key itself.</summary>
    private static string? Translate(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var text = config.GetLocalTxt(key);

        return string.IsNullOrWhiteSpace(text) || text == key ? null : text;
    }

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private void ReportOnce(string what, Exception ex)
    {
        if (_failures.IsNew($"{what}: {ex.GetType().FullName}: {ex.Message}")) _log($"Machines window: {what} failed (each distinct failure reported once): {ex}");
    }

    /// <summary>Every pot at home becomes one row: how many are growing, how many are ready, and the soonest ready.</summary>
    private sealed class PlantTally
    {
        private int _growing;
        private int _ready;
        private int? _soonest;

        public void Add(PlantComponent plant)
        {
            switch (plant.State)
            {
                case PlantState.Growing:
                    var remain = Math.Max(0, plant.GetRemainSeconds());

                    _growing++;
                    _soonest = _soonest is { } soonest ? Math.Min(soonest, remain) : remain;
                    break;
                case PlantState.Mature:
                    _ready++;
                    break;
            }
        }

        public MachineRow? Row()
        {
            if (_growing + _ready == 0) return null;

            var state = _ready > 0 ? MachineState.Finished : MachineState.Running;

            return new MachineRow(MachineKind.Plants, 0, "", new Contents.Plants(_growing, _ready), state, state == MachineState.Running ? _soonest : null, false, Icon: MachineIcons.KindIcon(MachineKind.Plants));
        }
    }
}
