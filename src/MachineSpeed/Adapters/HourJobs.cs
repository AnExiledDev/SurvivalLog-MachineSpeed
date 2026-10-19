using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;
using UnityEngine;

namespace MachineSpeed.Adapters;

/// <summary>One job the game times in whole hours, moved along a few times a second; false once it is over.</summary>
internal interface IHourJob
{
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

    private static readonly Dictionary<(string Kind, long Id), IHourJob> Jobs = new();
    private static HookFailures _failures = HookFailures.Silent;
    private static Speed _compost = Speed.GamePace;
    private static IntPtr _world;
    private static float _nextTick;
    private static float _nextBinScan;

    public static void UseLog(SpeedLog log) => _failures = new HookFailures(log.Write, "moving a brew, dye job or compost bin along");

    /// <summary>Bins already found move on at the new speed from the next tick. Set on the main thread, where the ticker runs.</summary>
    public static void UseCompost(Speed compost) => _compost = compost;

    public static void Track(string kind, long id, Func<IHourJob> start)
    {
        if (!Jobs.ContainsKey((kind, id))) Jobs[(kind, id)] = start();
    }

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
                CompostJob.FindBins(world, () => _compost);
            }

            StepAll(nowSeconds);
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
        _world = pointer;

        return false;
    }

    /// <summary>A job that fails is dropped, so it cannot fail every tick; it is picked up again at the game's next hour.</summary>
    private static void StepAll(int nowSeconds)
    {
        foreach (var (key, job) in Jobs.ToList())
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
        var brew = furniture == null ? null : AgentTools.GetAgentComponent<BrewComponent>(furniture);

        if (furniture != null && brew != null && brew.IsBrewing()) HourJobs.Track("brew", furniture.InstanceId, () => new BrewJob(furniture, brew, speed));
    }

    public bool Step(int nowSeconds)
    {
        if (_furniture.IsDead || !_brew.IsBrewing()) return false;

        var nowHour = Furniture.GetBrewNowHour();
        var elapsed = nowHour - _brew.StartHour;
        var moved = _clock.Elapsed(nowSeconds, elapsed, _brew.TotalHours, _speed());

        if (moved == elapsed) return true;

        _brew.StartHour = nowHour - moved;
        _first.Note(() => $"first brew moved along: {elapsed}h of {_brew.TotalHours}h done, now {moved}h");
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
        var dye = furniture == null ? null : AgentTools.GetAgentComponent<DyeComponent>(furniture);

        if (furniture != null && dye != null && dye.IsRunning()) HourJobs.Track("dye", furniture.InstanceId, () => new DyeJob(furniture, dye, speed));
    }

    public bool Step(int nowSeconds)
    {
        if (_furniture.IsDead || !_dye.IsRunning()) return false;

        var nowHour = Furniture.GetDyeNowHour();
        var total = _dye.ResultCount * _dye.HoursPerBucket;
        var elapsed = nowHour - _dye.StartHour;
        var moved = _clock.Elapsed(nowSeconds, elapsed, total, _speed());

        if (moved == elapsed) return true;

        _dye.StartHour = nowHour - moved;
        _first.Note(() => $"first dye job moved along: {elapsed}h of {total}h done, now {moved}h");
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
    private static FirstUse _first = FirstUse.Silent;

    private readonly Furniture _bin;
    private readonly Func<Speed> _speed;
    private readonly SpedUpClock _clock = new();

    private CompostJob(Furniture bin, Func<Speed> speed)
    {
        _bin = bin;
        _speed = speed;
    }

    public static void UseLog(SpeedLog log) => _first = new FirstUse(log.WriteFileOnly);

    public static void FindBins(BattleLogicWorld world, Func<Speed> speed)
    {
        var furnitures = world._AgentManager?.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture != null && !furniture.IsDead && CompostConfig.IsCompostBin(furniture.AgentConfigId))
            {
                HourJobs.Track("compost", furniture.InstanceId, () => new CompostJob(furniture, speed));
            }
        }
    }

    public bool Step(int nowSeconds)
    {
        if (_bin.IsDead) return false;

        var hours = _clock.ExtraHours(nowSeconds, _speed());

        if (hours == 0) return true;

        var items = GameSingleton.Existing<BattleLogicWorld>()?._ItemManager;
        var configs = GameSingleton.Existing<ConfigManager>();

        if (items == null || configs == null) return true;

        var nowHour = Furniture.GetDyeNowHour();
        var moved = 0;

        foreach (var item in ItemsIn(items, _bin.InstanceId))
        {
            var config = configs.Get_Config_Item(item.ItemConfigId);

            if (config == null || config.IT_Rot_Threshold <= 0f || config.Life <= 0 || item.OriginalTimeLeft <= 0) continue;

            item.StartTime -= hours;
            moved++;

            if (hours > 0) items.CheckAndProcessRot(item, config, nowHour, false);
        }

        if (moved > 0) _first.Note(() => $"first compost bin moved along: {moved} item(s) {hours}h on");

        return true;
    }

    /// <summary>A copy, because an item that rots is taken out of the bin's list while the rest are still being moved.</summary>
    private static List<ItemData> ItemsIn(ItemManager items, long ownerId)
    {
        var inBin = items.GetItemDataList(ownerId);
        var copy = new List<ItemData>();

        for (var i = 0; inBin != null && i < inBin.Count; i++)
        {
            if (inBin[i] is { } item) copy.Add(item);
        }

        return copy;
    }
}
