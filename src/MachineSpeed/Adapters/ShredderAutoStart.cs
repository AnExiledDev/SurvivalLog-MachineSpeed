using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Starts an idle shredder when the player puts items into its hopper. The game rebuilds the shredder's panel after
/// every hopper change, so the panel's item count is remembered per shredder and a change that raised it is a refill.
/// The start is the game's own Start button, so a shredder with no power or no room in its tray stays idle as usual.
/// The hooks go in the first time the setting is on.
/// </summary>
internal static class ShredderAutoStart
{
    private static readonly Dictionary<long, int> HopperCounts = new();

    private static Harmony? _harmony;
    private static SpeedLog? _log;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;
    private static bool _isOn;
    private static bool _isHooked;

    public static void Apply(Harmony harmony, SpeedLog log)
    {
        (_harmony, _log) = (harmony, log);
        (_failures, _first) = (new HookFailures(log.Write, "starting a refilled shredder", "Start button"), new FirstUse(log.WriteFileOnly));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(bool isOn)
    {
        _isOn = isOn;

        if (isOn && !_isHooked) Hook();

        _log?.Write(_isOn ? "a refilled shredder starts by itself" : "a refilled shredder waits for its Start button");
    }

    private static void Hook()
    {
        if (_harmony == null) return;

        try
        {
            _harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildShredderPanelData)), postfix: Machine.Patch(typeof(ShredderAutoStart), nameof(RememberCount)));
            _harmony.Patch(Machine.Method(typeof(Furniture), "OnShredderHopperChanged"), prefix: Machine.Patch(typeof(ShredderAutoStart), nameof(BeforeChange)),
                postfix: Machine.Patch(typeof(ShredderAutoStart), nameof(AfterChange)));
            _isHooked = true;
        }
        catch (Exception ex)
        {
            _isOn = false;
            _log?.Write($"{HookFailure.Prefix} the shredder's hopper, a refilled shredder waits for its Start button: {ex.Message}");
        }
    }

    private static void RememberCount(Furniture __instance, ShredderPanelData __result)
    {
        try
        {
            if (__instance != null && __result != null) HopperCounts[__instance.InstanceId] = __result.QueueTotalCount;
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void BeforeChange(Furniture __instance, out int? __state)
    {
        __state = null;

        try
        {
            if (__instance != null && HopperCounts.TryGetValue(__instance.InstanceId, out var count)) __state = count;
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    /// <summary>Runs after the game has turned a finished shredder back to idle and rebuilt its panel.</summary>
    private static void AfterChange(Furniture __instance, int? __state)
    {
        try
        {
            var shredder = __instance?._shredderComp;

            if (__instance == null || shredder == null || !HopperCounts.TryGetValue(__instance.InstanceId, out var now)) return;

            if (!ShredderRefill.ShouldStart(_isOn, shredder.IsIdle(), __state, now)) return;

            var isStarted = __instance.OnShredderStart();

            if (_first.IsPending) _first.Note($"first refilled shredder: {__state} to {now} items, started {isStarted}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
