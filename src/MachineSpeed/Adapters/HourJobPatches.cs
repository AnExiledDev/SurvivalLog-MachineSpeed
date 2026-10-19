using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// A brew is picked up when it starts, and at the game's hourly finish check, which also picks up brews from a loaded
/// save. Its panel times are the game's hours, so they are divided by the speed to match.
/// </summary>
internal static class BrewingPatch
{
    private static Speed _speed = Speed.GamePace;
    private static MachineHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        _failures = new HookFailures(log.Write, "speeding up a brew");
        BrewJob.UseLog(log);
        _hook = new MachineHook(log, "brewing",
            () =>
            {
                harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.OnBrewStart)), postfix: Machine.Patch(typeof(BrewingPatch), nameof(Started)));
                harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.TryFinishBrew)), postfix: Machine.Patch(typeof(BrewingPatch), nameof(Checked)));
            },
            () => harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildBrewPanelData)), postfix: Machine.Patch(typeof(BrewingPatch), nameof(ScalePanel))));
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

    private static void Checked(Furniture __instance)
    {
        try
        {
            BrewJob.TrackIfBrewing(__instance, () => _speed);
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
            if (__result == null) return;

            __result.TotalHours = Scaling.Shown(__result.TotalHours, _speed);
            __result.RemainHours = Scaling.Shown(__result.RemainHours, _speed);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
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
    private static MachineHook? _hook;
    private static HookFailures _failures = HookFailures.Silent;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        _failures = new HookFailures(log.Write, "speeding up the dye vat");
        DyeJob.UseLog(log);
        _hook = new MachineHook(log, "dye vat",
            () =>
            {
                harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.OnDyeStart)), postfix: Machine.Patch(typeof(DyeVatPatch), nameof(Started)));
                harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.TryFinishDye)), postfix: Machine.Patch(typeof(DyeVatPatch), nameof(Checked)));
            },
            () => harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildDyePanelData)), postfix: Machine.Patch(typeof(DyeVatPatch), nameof(ScalePanel))));
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

    private static void Checked(Furniture __instance)
    {
        try
        {
            DyeJob.TrackIfRunning(__instance, () => _speed);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void ScalePanel(DyePanelData __result)
    {
        try
        {
            if (__result == null) return;

            __result.HoursPerBucket = Scaling.Shown(__result.HoursPerBucket, _speed);
            __result.PreviewTotalHours = Scaling.Shown(__result.PreviewTotalHours, _speed);
            __result.CurrentBucketRemainSeconds = Scaling.Shown(__result.CurrentBucketRemainSeconds, _speed);
            __result.TotalRemainSeconds = Scaling.Shown(__result.TotalRemainSeconds, _speed);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>Compost bins have no start to hook; the ticker finds them. This hands the ticker the speed and logs it.</summary>
internal static class CompostPatch
{
    private static SpeedLog? _log;

    public static void Apply(SpeedLog log)
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
