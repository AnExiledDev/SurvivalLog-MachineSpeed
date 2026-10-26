using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;
using FurnitureList = Il2CppSystem.Collections.Generic.List<GameCore.HotUpdate.Battle.Logic.Furniture>;

namespace MachineSpeed.Adapters;

/// <summary>One machine found again just before it is rushed, with the clock it was read at.</summary>
internal sealed record FoundMachine(BattleLogicWorld World, Furniture Furniture, MachineRow Row, int NowSeconds);

/// <summary>
/// Reads every machine at home the way the game's own HUD finds furniture: placed, at home, on an unlocked floor and in
/// an unlocked area. Times are read from the machines' own fields, which Machine Speed's patches already keep at the
/// machine's speed. A machine that fails to read is left out, and each distinct failure is logged once.
/// The read with the window shut is spread over a few frames, about a millisecond of each, so a big base never costs
/// one frame the whole read. Names and icons come from the game's tables once per kind of furniture, and are asked
/// again only after another game is loaded, the tables are made again or the language changes.
/// </summary>
internal sealed class MachineReader
{
    private const int SecondsPerHour = 3600;
    private const int ShredderRunning = 1;
    private const int ShredderPaused = 2;
    private const int ShredderDone = 3;
    private const int MostPerSlice = 64;
    private static readonly long SliceTicks = Math.Max(1, Stopwatch.Frequency / 1000);

    private readonly Action<string> _log;
    private readonly DistinctFailures _failures = new();
    private readonly StampedCache<(IntPtr Config, string Language, bool IsChinese), (int ConfigId, MachineKind Kind), (string Name, string Icon)> _looks = new();
    private readonly StampedCache<(IntPtr Config, string Language, bool IsChinese), int, string?> _itemNames = new();
    private readonly StampedCache<(IntPtr Config, string Language, bool IsChinese), (MachineKind Kind, int ItemId), string?> _products = new();
    private readonly Pass _scan = new();
    private readonly HomeScan _home = new();
    private readonly Action<int> _visit;
    private readonly Func<bool> _isSpent;
    private readonly Func<bool> _isNeverSpent = () => false;
    private FurnitureList? _furnitures;
    private bool _isScanning;
    private long _sliceEnd;
    private IntPtr _lastWorld = IntPtr.Zero;

    public MachineReader(Action<string> log)
    {
        _log = log;
        _visit = VisitScanned;
        _isSpent = () => Stopwatch.GetTimestamp() >= _sliceEnd;
    }

    /// <summary>What is at home now, and the game clock in seconds (0 with no game), read whole in this frame.</summary>
    public (MachineRead Read, long NowSeconds) Read()
    {
        if (!BeginScan(out var early)) return (early!, 0);

        _home.Advance(int.MaxValue, _visit, _isNeverSpent);

        return (FinishScan(), _scan.Now);
    }

    /// <summary>
    /// Starts a read spread over frames. False when there is nothing to scan: <paramref name="read"/> is then the whole
    /// answer (no game, or away from home). A scan already under way is dropped.
    /// </summary>
    public bool BeginScan(out MachineRead? read)
    {
        _isScanning = false;
        _furnitures = null;
        _scan.End();
        read = null;

        if (LoadedGame() is not { } game)
        {
            read = new MachineRead.NoGame();

            return false;
        }

        var (world, config, agents) = game;

        if (!IsHome(agents))
        {
            read = new MachineRead.Away();

            return false;
        }

        UseWorld(world, config);
        _scan.Begin(world, config, agents);
        _furnitures = agents.GetAllFurnitures();
        _home.Start(_furnitures?.Count ?? 0);
        _isScanning = true;

        return true;
    }

    /// <summary>
    /// Reads this frame's share of the scan. True once the scan is over: <paramref name="read"/> is what is at home, or
    /// null when the scan was dropped because the game was left, another was loaded or home was left on the way.
    /// </summary>
    public bool ContinueScan(out MachineRead? read, out long nowSeconds)
    {
        read = null;
        nowSeconds = 0;

        if (!_isScanning || !IsStillThere()) return Drop();

        _sliceEnd = Stopwatch.GetTimestamp() + SliceTicks;

        if (!_home.Advance(MostPerSlice, _visit, _isSpent)) return false;

        nowSeconds = _scan.Now;
        read = FinishScan();

        return true;
    }

    /// <summary>The machine as it is right now, or null when it is gone, away from home or no longer that kind.</summary>
    public FoundMachine? Find(MachineKind kind, long id)
    {
        if (LoadedGame() is not { } game) return null;

        var (world, config, agents) = game;

        if (!IsHome(agents)) return null;

        UseWorld(world, config);

        var pass = new Pass();

        pass.Begin(world, config, agents);

        var furnitures = agents.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture._IsDead_k__BackingField || furniture._InstanceId_k__BackingField != id) continue;

            var row = RowOf(pass, furniture, null);

            return row != null && row.Kind == kind ? new FoundMachine(world, furniture, row, pass.Now) : null;
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

    /// <summary>
    /// Another world or other tables drop what was learned of the old ones; the language goes into the stamp, so names
    /// shown in one language are never shown after a switch to another.
    /// </summary>
    private void UseWorld(BattleLogicWorld world, ConfigManager config)
    {
        if (world.Pointer != _lastWorld)
        {
            _lastWorld = world.Pointer;
            FurnitureParts.Forget();
        }

        var stamp = (config.Pointer, GameLanguage.Key(), GameLanguage.IsChinese());

        _looks.Use(stamp);
        _itemNames.Use(stamp);
        _products.Use(stamp);
    }

    /// <summary>The scan goes on only in the same loaded game, at home, with the same tables.</summary>
    private bool IsStillThere() =>
        LoadedGame() is { } game && game.World.Pointer == _scan.WorldPointer && game.Config.Pointer == _scan.ConfigPointer && IsHome(game.Agents);

    private bool Drop()
    {
        _isScanning = false;
        _furnitures = null;
        _scan.End();

        return true;
    }

    private MachineRead FinishScan()
    {
        var read = new MachineRead.Home(_home.Rows(), Points(_scan.World));

        Drop();

        return read;
    }

    /// <summary>A piece counts as read before it is read, so one that throws is skipped, not read again.</summary>
    private void VisitScanned(int index)
    {
        try
        {
            var furniture = _furnitures![index];

            if (furniture == null || furniture._IsDead_k__BackingField) return;

            if (RowOf(_scan, furniture, _home) is { } row) _home.Add(row);
        }
        catch (Exception ex)
        {
            ReportOnce("machine read", ex);
        }
    }

    /// <summary>
    /// A drone out on a trip is hidden, so only it skips the shown check. Whether a floor is home and unlocked is asked
    /// once per floor in a read.
    /// </summary>
    private static bool IsAtHome(Pass pass, Furniture furniture, bool isDrone)
    {
        if (!isDrone && !furniture._IsShow_k__BackingField) return false;

        var map = furniture._MapConfigId_k__BackingField;

        if (!pass.MapAtHome.TryGetValue(map, out var isHomeFloor))
        {
            isHomeFloor = pass.Agents.IsHomeMap(map) && FloorTagConstants.IsMapPointUnlocked(pass.World._TagManager, map);
            pass.MapAtHome[map] = isHomeFloor;
        }

        return isHomeFloor && AreaUnlockConstants.IsFurnitureAreaUnlocked(furniture);
    }

    /// <summary>
    /// The window's row for one piece of furniture, or null when it is not a listed machine. Plants go into
    /// <paramref name="plants"/> when there is one. Which kind a piece is comes from its parts, in the order the window
    /// has always checked them; only that kind's part is then asked for.
    /// </summary>
    private MachineRow? RowOf(Pass pass, Furniture furniture, HomeScan? plants)
    {
        var parts = FurnitureParts.Of(furniture);

        if (MachinePartsOrder.KindOf(parts) is not { } kind) return null;

        if (!IsAtHome(pass, furniture, isDrone: kind == MachineKind.Drone)) return null;

        switch (kind)
        {
            case MachineKind.Drone:
                return AgentTools.GetAgentComponent<DroneTripComponent>(furniture) is { } drone ? DroneRow(pass, furniture, drone) : null;
            case MachineKind.Compost:
                return CompostRow(pass, furniture);
            case MachineKind.Shredder:
                if (AgentTools.GetAgentComponent<ShredderComponent>(furniture) is not { } shredder) return null;

                var state = shredder.State;
                var product = state == 0 ? null : ItemName(pass, shredder.HeadConfigId);

                return TimedRow(MachineKind.Shredder, pass, furniture, state, shredder.GetRemainSeconds(pass.Now), shredder.TotalSeconds, product);
            case MachineKind.RationPress:
                return AgentTools.GetAgentComponent<CompressorComponent>(furniture) is { } press
                    ? TimedRow(MachineKind.RationPress, pass, furniture, press.State, press.GetRemainSeconds(pass.Now), press.TotalSeconds, null)
                    : null;
            case MachineKind.DyeVat:
                return AgentTools.GetAgentComponent<DyeComponent>(furniture) is { } dye ? DyeRow(pass, furniture, dye) : null;
            case MachineKind.Brewing:
                return AgentTools.GetAgentComponent<BrewComponent>(furniture) is { } brew ? BrewRow(pass, furniture, brew) : null;
            case MachineKind.Cooking:
                return AgentTools.GetAgentComponent<CookingComponent>(furniture) is { } cooking ? CookingRow(pass, furniture, cooking) : null;
            case MachineKind.Generator:
                if (AgentTools.GetAgentComponent<GeneratorFuelComponent>(furniture) is { IsFuelHeater: false } generator) return GeneratorRow(pass, furniture, generator);

                // A fuel heater is not listed as a generator; a heater that is also a pot still counts as a pot.
                if (MachinePartsOrder.KindOfHeater(parts) == MachineKind.Plants) AddPlant(furniture, plants);

                return null;
            case MachineKind.Plants:
                AddPlant(furniture, plants);

                return null;
            default:
                return null;
        }
    }

    private static void AddPlant(Furniture furniture, HomeScan? plants)
    {
        if (plants == null || AgentTools.GetAgentComponent<PlantComponent>(furniture) is not { } plant) return;

        switch (plant.State)
        {
            case PlantState.Growing:
                plants.AddGrowing(plant.GetRemainSeconds());
                break;
            case PlantState.Mature:
                plants.AddReady();
                break;
        }
    }

    /// <summary>
    /// The shredder and the Ration Press share their states: 1 running, 2 paused (power off), 3 done. A running batch's
    /// TotalSeconds is already at the machine's speed.
    /// </summary>
    private MachineRow TimedRow(MachineKind kind, Pass pass, Furniture furniture, int state, int remain, int total, string? product)
    {
        var (shown, isTimed) = state switch
        {
            ShredderRunning => (MachineState.Running, true),
            ShredderPaused => (MachineState.Paused, true),
            ShredderDone => (MachineState.Finished, false),
            _ => (MachineState.Idle, false),
        };

        return Row(kind, pass, furniture, product, shown, isTimed ? remain : null, canRush: shown == MachineState.Running, isTimed ? total : null);
    }

    private MachineRow BrewRow(Pass pass, Furniture furniture, BrewComponent brew)
    {
        var now = pass.Now;

        if (brew.IsBrewing())
        {
            var nowHour = now / SecondsPerHour;
            var done = Countdown.Done(nowHour - brew.StartHour, now, nowHour, HourJobs.Carry("brew", furniture._InstanceId_k__BackingField));
            var totalHours = brew.TotalHours;
            var remain = Countdown.Remaining(totalHours, done, BrewingPatch.Speed);
            var total = Countdown.Remaining(totalHours, 0, BrewingPatch.Speed);

            return Row(MachineKind.Brewing, pass, furniture, Product(MachineKind.Brewing, brew.ResultItemId), MachineState.Running, remain, canRush: true, total);
        }

        return brew.IsDone()
            ? Row(MachineKind.Brewing, pass, furniture, Product(MachineKind.Brewing, brew.ResultItemId), MachineState.Finished, null, canRush: false)
            : Row(MachineKind.Brewing, pass, furniture, null, MachineState.Idle, null, canRush: false);
    }

    private MachineRow DyeRow(Pass pass, Furniture furniture, DyeComponent dye)
    {
        var now = pass.Now;

        if (dye.IsRunning())
        {
            var nowHour = now / SecondsPerHour;
            var done = Countdown.Done(nowHour - dye.StartHour, now, nowHour, HourJobs.Carry("dye", furniture._InstanceId_k__BackingField));
            var totalHours = dye.ResultCount * dye.HoursPerBucket;
            var remain = Countdown.Remaining(totalHours, done, DyeVatPatch.Speed);
            var total = Countdown.Remaining(totalHours, 0, DyeVatPatch.Speed);

            return Row(MachineKind.DyeVat, pass, furniture, Product(MachineKind.DyeVat, dye.ResultItemId), MachineState.Running, remain, canRush: true, total);
        }

        return dye.IsDone()
            ? Row(MachineKind.DyeVat, pass, furniture, Product(MachineKind.DyeVat, dye.ResultItemId), MachineState.Finished, null, canRush: false)
            : Row(MachineKind.DyeVat, pass, furniture, null, MachineState.Idle, null, canRush: false);
    }

    /// <summary>A dish waiting to be taken out leaves the stove idle with a pending product.</summary>
    private MachineRow CookingRow(Pass pass, Furniture furniture, CookingComponent cooking)
    {
        var start = cooking.CookStartTotalSeconds;

        return cooking.State switch
        {
            CookingState.Running => Row(MachineKind.Cooking, pass, furniture, null, MachineState.Running, Math.Max(0, cooking.CookDuration - (pass.Now - start)), canRush: true, cooking.CookDuration),
            CookingState.Paused => Row(MachineKind.Cooking, pass, furniture, null, MachineState.Paused, Math.Max(0, cooking.CookDuration - (cooking.PauseStartTotalSeconds - start)), canRush: false, cooking.CookDuration),
            _ when cooking.HasPendingProduct => Row(MachineKind.Cooking, pass, furniture, NonEmpty(cooking.PendingProductName), MachineState.Finished, null, canRush: false),
            _ => Row(MachineKind.Cooking, pass, furniture, null, MachineState.Idle, null, canRush: false),
        };
    }

    /// <summary>
    /// A drone still out is running until its return time; one back with cargo not yet unloaded is finished. While a
    /// trader's visit holds the trade screen, a trip is not cut short, so the game's trade is never pulled from under it.
    /// </summary>
    private MachineRow DroneRow(Pass pass, Furniture furniture, DroneTripComponent drone)
    {
        var now = pass.Now;
        var isBusy = drone.IsBusy();
        var returnSec = drone.ReturnSec;

        if (isBusy && returnSec > now)
        {
            var isTradeLocked = pass.World._StrangerTradeActiveManager?._IsLocked_k__BackingField != false;

            return Row(MachineKind.Drone, pass, furniture, null, MachineState.Running, returnSec - now, canRush: !isTradeLocked, returnSec - drone.DepartSec);
        }

        var hasCargo = isBusy || StrangerTradeActiveManager.HasCargoLeftover(furniture._InstanceId_k__BackingField);

        return Row(MachineKind.Drone, pass, furniture, null, hasCargo ? MachineState.Finished : MachineState.Idle, null, canRush: false);
    }

    /// <summary>A generator that is switched off is shown as paused: its fuel is not burning.</summary>
    private MachineRow GeneratorRow(Pass pass, Furniture furniture, GeneratorFuelComponent generator)
    {
        if (!generator.HasFuel()) return Row(MachineKind.Generator, pass, furniture, null, MachineState.OutOfFuel, null, canRush: false);

        var isOn = pass.World._PowerManager?.GetGeneratorState(furniture._InstanceId_k__BackingField) == true;

        return Row(MachineKind.Generator, pass, furniture, null, isOn ? MachineState.Running : MachineState.Paused, generator.GetRemainingGameSeconds(), canRush: false);
    }

    /// <summary>A bin is working while it holds anything; it has no single finish time.</summary>
    private MachineRow CompostRow(Pass pass, Furniture furniture)
    {
        var id = furniture._InstanceId_k__BackingField;
        var items = pass.World._ItemManager?.GetItemDataList(id);
        var count = 0;

        for (var i = 0; items != null && i < items.Count; i++)
        {
            var item = items[i];

            if (item != null) count += item.ItemCount <= 0 ? 1 : item.ItemCount;
        }

        var (name, icon) = Looks(pass, MachineKind.Compost, furniture);

        return count > 0
            ? new MachineRow(MachineKind.Compost, id, name, new Contents.Items(count), MachineState.Running, null, false, Icon: icon)
            : new MachineRow(MachineKind.Compost, id, name, null, MachineState.Idle, null, false, Icon: icon);
    }

    private MachineRow Row(MachineKind kind, Pass pass, Furniture furniture, string? product, MachineState state, double? remain, bool canRush, double? total = null)
    {
        var (name, icon) = Looks(pass, kind, furniture);

        return new MachineRow(kind, furniture._InstanceId_k__BackingField, name, product == null ? null : new Contents.Named(product), state, remain, canRush, total, icon);
    }

    /// <summary>A kind of furniture's name and icon, from its table row; an empty name when it has none.</summary>
    private (string Name, string Icon) Looks(Pass pass, MachineKind kind, Furniture furniture)
    {
        var configId = furniture._AgentConfigId_k__BackingField;

        if (_looks.TryGet((configId, kind), out var looks)) return looks;

        var row = pass.Config.Get_Config_Furniture(configId);
        var name = row == null ? "" : Translate(pass.Config, row.Name) ?? NonEmpty(row.Name_Local) ?? NonEmpty(row.Name) ?? "";

        looks = (name, MachineIcons.Of(kind, row?.ICON));
        _looks.Set((configId, kind), looks);

        return looks;
    }

    private string? ItemName(Pass pass, int itemId)
    {
        if (_itemNames.TryGet(itemId, out var name)) return name;

        var item = pass.Config.Get_Config_Item(itemId);

        name = item == null ? null : Translate(pass.Config, item.ItemName) ?? NonEmpty(item.ItemName_Local) ?? NonEmpty(item.ItemName);
        _itemNames.Set(itemId, name);

        return name;
    }

    private string? Product(MachineKind kind, int resultItemId)
    {
        if (_products.TryGet((kind, resultItemId), out var name)) return name;

        name = NonEmpty(kind == MachineKind.Brewing ? Furniture.GetBrewProductName(resultItemId) : Furniture.GetDyeProductName(resultItemId));
        _products.Set((kind, resultItemId), name);

        return name;
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

    /// <summary>What one read keeps from its start: the game it reads, its clock, and which floors are home.</summary>
    private sealed class Pass
    {
        public readonly Dictionary<int, bool> MapAtHome = new();
        public BattleLogicWorld World = null!;
        public ConfigManager Config = null!;
        public AgentManager Agents = null!;
        public IntPtr WorldPointer;
        public IntPtr ConfigPointer;
        public int Now;

        public void Begin(BattleLogicWorld world, ConfigManager config, AgentManager agents)
        {
            World = world;
            Config = config;
            Agents = agents;
            WorldPointer = world.Pointer;
            ConfigPointer = config.Pointer;
            Now = world._GameTimeManager.GetTotalSeconds();
            MapAtHome.Clear();
        }

        /// <summary>Lets go of the game's objects, so a finished or dropped read holds nothing of a world.</summary>
        public void End()
        {
            World = null!;
            Config = null!;
            Agents = null!;
            WorldPointer = IntPtr.Zero;
            ConfigPointer = IntPtr.Zero;
            MapAtHome.Clear();
        }
    }
}
