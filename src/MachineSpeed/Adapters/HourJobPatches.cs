using System;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// A brew is picked up when it starts, and at the game's hourly finish check, which also picks up brews from a loaded
/// save. Its panel times are the game's hours, so they are divided by the speed to match.
/// </summary>
internal static class BrewingPatch
{
    private static Speed _speed = Speed.GamePace;

    // One for every brew, read at each step so a change of the setting reaches brews already running.
    private static readonly Func<Speed> SpeedNow = () => _speed;
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up a brew, it keeps the game's duration", ex);
        BrewJob.UseLog(log);
        _hook = new MachineHook(log, "brewing",
            () =>
            {
                hooks.PatchOrThrow(typeof(BrewingPatch), typeof(Furniture), nameof(Furniture.OnBrewStart), postfix: nameof(Started));
                hooks.PatchOrThrow(typeof(BrewingPatch), typeof(Furniture), nameof(Furniture.TryFinishBrew), postfix: nameof(Checked));
            },
            () =>
            {
                hooks.PatchOrThrow(typeof(BrewingPatch), typeof(Furniture), nameof(Furniture.BuildBrewPanelData), postfix: nameof(ScalePanel));
                hooks.PatchOrThrow(typeof(BrewingPatch), typeof(Furniture), nameof(Furniture.SyncBrewHeadBar), postfix: nameof(ScaleHeadBar));
            });
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _hook?.Use(speed);
    }

    private static void Started(Furniture __instance, bool __result)
    {
        if (__result) Checked(__instance);
    }

    /// <summary>
    /// The game's hourly check runs this for every piece of furniture there is, so anything that is not a barrel, or a
    /// barrel whose brew is already being moved along, is passed over before the game is asked anything about it.
    /// </summary>
    private static void Checked(Furniture __instance)
    {
        try
        {
            if (__instance == null || !HourJobLook.ShouldLook(FurnitureParts.Of(__instance), MachineParts.Brewing, HourJobs.IsTracked("brew", __instance._InstanceId_k__BackingField))) return;

            BrewJob.TrackIfBrewing(__instance, SpeedNow);
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }

    public static Speed Speed => _speed;

    private static void ScalePanel(Furniture __instance, BrewPanelData __result)
    {
        try
        {
            if (__result == null) return;

            TimerPage.ShowingBrew(__instance, __result.TotalHours);

            __result.TotalHours = Scaling.Shown(__result.TotalHours, _speed);
            __result.RemainHours = Scaling.Shown(__result.RemainHours, _speed);
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }

    /// <summary>
    /// The game sends the barrel's timer the brew's game hours, at the start and every hour, so a sped-up brew read as
    /// days. It is sent again with the sped-up time whenever the game has just sent a running brew's.
    /// </summary>
    private static void ScaleHeadBar(Furniture __instance)
    {
        try
        {
            if (_speed.IsGamePace || __instance._brewHeadBarState != GameHeadBar.Running) return;

            var brew = AgentTools.GetAgentComponent<BrewComponent>(__instance);

            if (brew == null || !brew.IsBrewing()) return;

            var nowHour = Furniture.GetBrewNowHour();
            var done = Countdown.Done(nowHour - brew.StartHour, Furniture.GetDyeNowSeconds(), nowHour, HourJobs.Carry("brew", __instance._InstanceId_k__BackingField));

            GameHeadBar.Show(__instance, HeadBar.Job(brew.TotalHours, done, _speed), Furniture.GetBrewProductName(brew.ResultItemId));
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }
}

/// <summary>
/// A dye job is picked up when it starts, and at the game's hourly bucket check, which also picks up jobs from a
/// loaded save. Its panel times are the game's, so they are divided by the speed to match.
/// </summary>
internal static class DyeVatPatch
{
    private static Speed _speed = Speed.GamePace;

    // One for every dye job, read at each step so a change of the setting reaches jobs already running.
    private static readonly Func<Speed> SpeedNow = () => _speed;
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up the dye vat, it keeps the game's duration", ex);
        DyeJob.UseLog(log);
        _hook = new MachineHook(log, "dye vat",
            () =>
            {
                hooks.PatchOrThrow(typeof(DyeVatPatch), typeof(Furniture), nameof(Furniture.OnDyeStart), postfix: nameof(Started));
                hooks.PatchOrThrow(typeof(DyeVatPatch), typeof(Furniture), nameof(Furniture.TryFinishDye), postfix: nameof(Checked));
            },
            () =>
            {
                hooks.PatchOrThrow(typeof(DyeVatPatch), typeof(Furniture), nameof(Furniture.BuildDyePanelData), postfix: nameof(ScalePanel));
                hooks.PatchOrThrow(typeof(DyeVatPatch), typeof(Furniture), nameof(Furniture.SyncDyeHeadBar), postfix: nameof(ScaleHeadBar));
            });
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _hook?.Use(speed);
    }

    private static void Started(Furniture __instance, bool __result)
    {
        if (__result) Checked(__instance);
    }

    /// <summary>
    /// The game's hourly check runs this for every piece of furniture there is, so anything that is not a vat, or a vat
    /// whose job is already being moved along, is passed over before the game is asked anything about it.
    /// </summary>
    private static void Checked(Furniture __instance)
    {
        try
        {
            if (__instance == null || !HourJobLook.ShouldLook(FurnitureParts.Of(__instance), MachineParts.DyeVat, HourJobs.IsTracked("dye", __instance._InstanceId_k__BackingField))) return;

            DyeJob.TrackIfRunning(__instance, SpeedNow);
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }

    public static Speed Speed => _speed;

    private static void ScalePanel(Furniture __instance, DyePanelData __result)
    {
        try
        {
            if (__result == null) return;

            TimerPage.ShowingDye(__instance, __result.HoursPerBucket, __result.PreviewTotalHours);

            __result.HoursPerBucket = Scaling.Shown(__result.HoursPerBucket, _speed);
            __result.PreviewTotalHours = Scaling.Shown(__result.PreviewTotalHours, _speed);
            __result.CurrentBucketRemainSeconds = Scaling.Shown(__result.CurrentBucketRemainSeconds, _speed);
            __result.TotalRemainSeconds = Scaling.Shown(__result.TotalRemainSeconds, _speed);
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }

    /// <summary>
    /// The game sends the vat's timer the bucket's game hours, so a sped-up bucket read as hours. It is sent again with
    /// the sped-up time whenever the game has just sent a running bucket's (not when a bucket is ready to take out).
    /// </summary>
    private static void ScaleHeadBar(Furniture __instance)
    {
        try
        {
            if (_speed.IsGamePace || __instance._dyeHeadBarState != GameHeadBar.Running) return;

            var dye = AgentTools.GetAgentComponent<DyeComponent>(__instance);

            if (dye == null || !dye.IsRunning()) return;

            var nowHour = Furniture.GetDyeNowHour();
            var done = Countdown.Done(nowHour - dye.StartHour, Furniture.GetDyeNowSeconds(), nowHour, HourJobs.Carry("dye", __instance._InstanceId_k__BackingField));

            GameHeadBar.Show(__instance, HeadBar.Bucket(dye.HoursPerBucket, dye.ResultCount, done, _speed), Furniture.GetDyeProductName(dye.ResultItemId));
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }
}

/// <summary>The timer over a machine, which the game draws from a plant growth event with its length and progress.</summary>
internal static class GameHeadBar
{
    /// <summary>The value the game keeps for a machine whose timer is showing a running job.</summary>
    public const int Running = 1;

    public static void Show(Furniture machine, HeadBar bar, string name) =>
        EventDispatcherTools.NotifyInstance<float, float, int, string>(machine.ShowInstanceId, EnumNotify_Plant.PlantGrowthUpdate, bar.Total, bar.Elapsed, 0, name);
}

/// <summary>Compost bins have no start to hook; the ticker finds them. This hands the ticker the speed and logs it.</summary>
internal static class CompostPatch
{
    private static ModLog? _log;

    public static void Apply(ModLog log)
    {
        _log = log;
        CompostJob.UseLog(log);
    }

    public static void Use(Speed speed)
    {
        HourJobs.UseCompost(speed);
        _log?.Write(speed.IsGamePace ? "compost bins left at the game's pace" : $"compost bins x{speed.Value:0.##}");
    }
}
