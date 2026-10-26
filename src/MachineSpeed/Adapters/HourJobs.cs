using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;
using UnityEngine;

namespace MachineSpeed.Adapters;

/// <summary>One job the game times in whole hours, moved along a few times a second; false once it is over.</summary>
internal interface IHourJob
{
    /// <summary>Game seconds of sped-up time built up that the game has not been handed yet.</summary>
    double CarrySeconds { get; }

    bool Step(int nowSeconds);
}

/// <summary>
/// Brews, dye jobs and compost bins that are being moved along. They are dropped when the job ends and all at once
/// when the world goes away (a save loading, the title screen), so nothing from an old world is ever touched.
/// </summary>
internal static class HourJobs
{
    private const float TickSeconds = 0.25f;
    private const float BinScanSeconds = 5f;
    private const float PlantScanSeconds = 5f;

    private static readonly Dictionary<(string Kind, long Id), IHourJob> Jobs = new();

    // Kept and emptied after each step, so stepping the jobs makes no new list four times a second.
    private static readonly List<KeyValuePair<(string Kind, long Id), IHourJob>> Stepping = new();
    private static HookFailures _failures = HookFailures.Silent;
    private static Speed _compost = Speed.GamePace;
    private static readonly Func<Speed> CompostSpeed = () => _compost;
    private static IntPtr _world;
    private static float _nextTick;

    // The bin and pot scans start apart, so the two never land on the same frame.
    private static float _nextBinScan = 1.25f;
    private static float _nextPlantScan = 3.75f;

    public static void UseLog(SpeedLog log) => _failures = new HookFailures(log.Write, "moving a brew, dye job or compost bin along");

    /// <summary>Bins already found move on at the new speed from the next tick. Set on the main thread, where the ticker runs.</summary>
    public static void UseCompost(Speed compost) => _compost = compost;

    public static bool IsTracked(string kind, long id) => Jobs.ContainsKey((kind, id));

    /// <summary>Starts moving a job along; one already moving is kept as it is.</summary>
    public static void Add(string kind, long id, IHourJob job) => Jobs.TryAdd((kind, id), job);

    /// <summary>The time a job is carrying toward its next hour; 0 for a job not being moved along.</summary>
    public static double Carry(string kind, long id) => Jobs.TryGetValue((kind, id), out var job) ? job.CarrySeconds : 0;

    public static void Tick(float realSeconds)
    {
        if (realSeconds < _nextTick) return;

        _nextTick = realSeconds + TickSeconds;

        try
        {
            var world = GameSingleton.Existing<BattleLogicWorld>();

            if (!IsSameWorld(world) || world == null) return;

            var nowSeconds = Furniture.GetDyeNowSeconds();

            if (!_compost.IsGamePace && realSeconds >= _nextBinScan)
            {
                _nextBinScan = realSeconds + BinScanSeconds;
                CompostJob.FindBins(world, CompostSpeed);
            }

            if (PlantGrowthPatch.IsIn && realSeconds >= _nextPlantScan)
            {
                _nextPlantScan = realSeconds + PlantScanSeconds;
                PlantCaches.Resync(world, PlantGrowthPatch.Speed);
            }

            StepAll(nowSeconds);
            TimerPage.Tick(realSeconds, nowSeconds);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static bool IsSameWorld(BattleLogicWorld? world)
    {
        var pointer = world?.Pointer ?? IntPtr.Zero;

        if (pointer == _world) return true;

        Jobs.Clear();
        FurnitureParts.Forget();
        PlantCaches.Forget();
        TimerPage.Forget();
        CookingPatch.Forget();
        _world = pointer;

        return false;
    }

    /// <summary>A job that fails is dropped, so it cannot fail every tick; it is picked up again at the game's next hour.</summary>
    private static void StepAll(int nowSeconds)
    {
        Stepping.AddRange(Jobs);

        try
        {
            foreach (var (key, job) in Stepping)
            {
                bool isRunning;

                try
                {
                    isRunning = job.Step(nowSeconds);
                }
                catch (Exception ex)
                {
                    isRunning = false;
                    _failures.Report(ex);
                }

                if (!isRunning) Jobs.Remove(key);
            }
        }
        finally
        {
            Stepping.Clear();
        }
    }
}

/// <summary>Hands frames to <see cref="HourJobs"/>.</summary>
public sealed class MachineTicker : MonoBehaviour
{
    public MachineTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update() => HourJobs.Tick(Time.realtimeSinceStartup);
}

/// <summary>
/// A brew finishes once its whole hours have passed since its start hour, checked by the game once an hour. Moving
/// the start hour back moves the brew on; the game's own finish is then asked for at once.
/// </summary>
internal sealed class BrewJob : IHourJob
{
    private static FirstUse _first = FirstUse.Silent;

    private readonly Furniture _furniture;
    private readonly BrewComponent _brew;
    private readonly Func<Speed> _speed;
    private readonly SpedUpClock _clock = new();

    public double CarrySeconds => _clock.CarrySeconds;

    private BrewJob(Furniture furniture, BrewComponent brew, Func<Speed> speed)
    {
        _furniture = furniture;
        _brew = brew;
        _speed = speed;
    }

    public static void UseLog(SpeedLog log) => _first = new FirstUse(log.WriteFileOnly);

    /// <summary>The speed is read at every step, so a brew already running follows a change of the setting.</summary>
    public static void TrackIfBrewing(Furniture? furniture, Func<Speed> speed)
    {
        if (furniture == null) return;

        var id = furniture._InstanceId_k__BackingField;

        if (HourJobs.IsTracked("brew", id)) return;

        var brew = AgentTools.GetAgentComponent<BrewComponent>(furniture);

        if (brew != null && brew.IsBrewing()) HourJobs.Add("brew", id, new BrewJob(furniture, brew, speed));
    }

    public bool Step(int nowSeconds)
    {
        if (_furniture._IsDead_k__BackingField || !_brew.IsBrewing()) return false;

        var nowHour = Furniture.GetBrewNowHour();
        var elapsed = nowHour - _brew.StartHour;
        var moved = _clock.Elapsed(nowSeconds, elapsed, _brew.TotalHours, _speed());

        if (moved == elapsed) return true;

        _brew.StartHour = nowHour - moved;
        if (_first.IsPending) _first.Note($"first brew moved along: {elapsed}h of {_brew.TotalHours}h done, now {moved}h");
        _furniture.TryFinishBrew();

        return _brew.IsBrewing();
    }
}

/// <summary>
/// A dye job finishes a bucket each time its hours per bucket pass since its start hour, checked by the game once an
/// hour. Moving the start hour back moves every bucket on; the game's own check is then asked for at once.
/// </summary>
internal sealed class DyeJob : IHourJob
{
    private static FirstUse _first = FirstUse.Silent;

    private readonly Furniture _furniture;
    private readonly DyeComponent _dye;
    private readonly Func<Speed> _speed;
    private readonly SpedUpClock _clock = new();

    public double CarrySeconds => _clock.CarrySeconds;

    private DyeJob(Furniture furniture, DyeComponent dye, Func<Speed> speed)
    {
        _furniture = furniture;
        _dye = dye;
        _speed = speed;
    }

    public static void UseLog(SpeedLog log) => _first = new FirstUse(log.WriteFileOnly);

    /// <summary>The speed is read at every step, so a dye job already running follows a change of the setting.</summary>
    public static void TrackIfRunning(Furniture? furniture, Func<Speed> speed)
    {
        if (furniture == null) return;

        var id = furniture._InstanceId_k__BackingField;

        if (HourJobs.IsTracked("dye", id)) return;

        var dye = AgentTools.GetAgentComponent<DyeComponent>(furniture);

        if (dye != null && dye.IsRunning()) HourJobs.Add("dye", id, new DyeJob(furniture, dye, speed));
    }

    public bool Step(int nowSeconds)
    {
        if (_furniture._IsDead_k__BackingField || !_dye.IsRunning()) return false;

        var nowHour = Furniture.GetDyeNowHour();
        var total = _dye.ResultCount * _dye.HoursPerBucket;
        var elapsed = nowHour - _dye.StartHour;
        var moved = _clock.Elapsed(nowSeconds, elapsed, total, _speed());

        if (moved == elapsed) return true;

        _dye.StartHour = nowHour - moved;
        if (_first.IsPending) _first.Note($"first dye job moved along: {elapsed}h of {total}h done, now {moved}h");
        _furniture.TryFinishDye();

        return _dye.IsRunning();
    }
}

/// <summary>
/// Food in a compost bin rots by the hours since its start time, checked by the game once an hour, and rotted food
/// turns into compost there. Moving the start time back rots it sooner; the game's own rot check is then asked for at
/// once. Only food the game would rot is moved, the same test its hourly rot check makes.
/// </summary>
internal sealed class CompostJob : IHourJob
{
    private static readonly List<ItemData> InBin = new();
    private static FirstUse _first = FirstUse.Silent;

    private readonly Furniture _bin;
    private readonly Func<Speed> _speed;
    private readonly SpedUpClock _clock = new();

    public double CarrySeconds => _clock.CarrySeconds;

    private CompostJob(Furniture bin, Func<Speed> speed)
    {
        _bin = bin;
        _speed = speed;
    }

    public static void UseLog(SpeedLog log) => _first = new FirstUse(log.WriteFileOnly);

    /// <summary>Whether a piece is a bin is known once per kind of furniture, so the scan asks the game nothing of other pieces.</summary>
    public static void FindBins(BattleLogicWorld world, Func<Speed> speed)
    {
        var furnitures = world._AgentManager?.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture._IsDead_k__BackingField || (FurnitureParts.Of(furniture) & MachineParts.CompostBin) == 0) continue;

            var id = furniture._InstanceId_k__BackingField;

            if (!HourJobs.IsTracked("compost", id)) HourJobs.Add("compost", id, new CompostJob(furniture, speed));
        }
    }

    public bool Step(int nowSeconds)
    {
        if (_bin._IsDead_k__BackingField) return false;

        var hours = _clock.ExtraHours(nowSeconds, _speed());

        if (hours == 0) return true;

        var items = GameSingleton.Existing<BattleLogicWorld>()?._ItemManager;
        var configs = GameSingleton.Existing<ConfigManager>();

        if (items == null || configs == null) return true;

        var nowHour = Furniture.GetDyeNowHour();
        var moved = 0;

        ItemsIn(items, _bin._InstanceId_k__BackingField);

        try
        {
            foreach (var item in InBin)
            {
                var config = configs.Get_Config_Item(item.ItemConfigId);

                if (config == null || config.IT_Rot_Threshold <= 0f || config.Life <= 0 || item.OriginalTimeLeft <= 0) continue;

                item.StartTime -= hours;
                moved++;

                if (hours > 0) items.CheckAndProcessRot(item, config, nowHour, false);
            }
        }
        finally
        {
            InBin.Clear();
        }

        if (moved > 0 && _first.IsPending) _first.Note($"first compost bin moved along: {moved} item(s) {hours}h on");

        return true;
    }

    /// <summary>
    /// A copy, because an item that rots is taken out of the bin's list while the rest are still being moved. One list
    /// serves every bin, emptied after each.
    /// </summary>
    private static void ItemsIn(ItemManager items, long ownerId)
    {
        var inBin = items.GetItemDataList(ownerId);

        InBin.Clear();

        for (var i = 0; inBin != null && i < inBin.Count; i++)
        {
            if (inBin[i] is { } item) InBin.Add(item);
        }
    }
}
