using System;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// The game works out a brew's length in whole game hours in one place, the brew preview, and both the brewing panel
/// and the start of a brew read it from there, so shortening it there keeps the hours the panel shows equal to the
/// hours the brew takes. If that hook cannot be installed, the start of the brew is hooked instead and only the
/// panel's estimate stays at the game's.
/// </summary>
internal static class BrewingPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up a brew"), new FirstUse(log.WriteFileOnly));

        if (speed.IsGamePace)
        {
            log.Write("brewing left at the game's pace");

            return;
        }

        try
        {
            harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.CalcBrewPreview)), postfix: Machine.Patch(typeof(BrewingPatch), nameof(Scale)));
            log.Write($"brewing x{speed.Value:0.##}");
        }
        catch (Exception previewFailure)
        {
            log.Write($"could not hook the brewing panel, it shows the game's hours: {previewFailure.Message}");
            Machine.Hook(log, "brewing", speed, () =>
                harmony.Patch(Machine.Method(typeof(BrewComponent), nameof(BrewComponent.BeginBrew)), prefix: Machine.Patch(typeof(BrewingPatch), nameof(Scale))));
        }
    }

    /// <summary>A postfix on the preview (where totalHours is an out value) or a prefix on the start (where it is an argument).</summary>
    private static void Scale(ref int totalHours)
    {
        try
        {
            var gameHours = totalHours;
            var hours = Scaling.WholeUnits(gameHours, _speed);

            totalHours = hours;
            _first.Note(() => $"first brew: game {gameHours}h, now {hours}h");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
