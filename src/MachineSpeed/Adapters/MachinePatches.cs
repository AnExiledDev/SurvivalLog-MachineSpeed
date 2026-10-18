using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

// One small class per machine because Harmony patches are static and each machine keeps its own speed. Every patch
// runs inside the game's own call, so nothing may escape it.

/// <summary>The shredder asks the game's config for an item's shred time in whole minutes when it starts.</summary>
internal static class ShredderPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up the shredder"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "shredder", speed, () =>
            harmony.Patch(Machine.Method(typeof(ConfigManager), nameof(ConfigManager.GetShredMinutes)), postfix: Machine.Patch(typeof(ShredderPatch), nameof(Scale))));
    }

    private static void Scale(ref int __result)
    {
        try
        {
            var gameMinutes = __result;

            var minutes = Scaling.WholeUnits(gameMinutes, _speed);

            __result = minutes;
            _first.Note(() => $"first shred: game {gameMinutes} min, now {minutes} min");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>The ration press works out a batch, minutes included, in one formula that the panel and the press both read.</summary>
internal static class RationPressPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up the ration press"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "ration press", speed, () =>
            harmony.Patch(Machine.Method(typeof(CompressFormula), nameof(CompressFormula.Calc)), postfix: Machine.Patch(typeof(RationPressPatch), nameof(Scale))));
    }

    private static void Scale(CompressBatch __result)
    {
        try
        {
            if (__result == null) return;

            var gameMinutes = __result.Minutes;

            __result.Minutes = Scaling.WholeUnits(gameMinutes, _speed);
            _first.Note(() => $"first ration batch: game {gameMinutes} min, now {__result.Minutes} min");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// A dye job is timed in whole hours per bucket from the moment it begins. The idle vat's panel estimates the job
/// from the game's hours, so it is brought in step; a running vat's panel reads the job, which is already sped up.
/// </summary>
internal static class DyeVatPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up the dye vat"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "dye vat", speed,
            () => harmony.Patch(Machine.Method(typeof(DyeComponent), nameof(DyeComponent.Begin)), prefix: Machine.Patch(typeof(DyeVatPatch), nameof(ScaleJob))),
            () => harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildDyePanelData)), postfix: Machine.Patch(typeof(DyeVatPatch), nameof(ScalePanel))));
    }

    private static void ScaleJob(ref int hoursPerBucket)
    {
        try
        {
            var gameHours = hoursPerBucket;

            var hours = Scaling.WholeUnits(gameHours, _speed);

            hoursPerBucket = hours;
            _first.Note(() => $"first dye job: game {gameHours}h per bucket, now {hours}h");
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

            var panel = Scaling.Dye(new DyePreview(__result.State, __result.BucketCount, __result.HoursPerBucket, __result.PreviewTotalHours), _speed);

            __result.HoursPerBucket = panel.HoursPerBucket;
            __result.PreviewTotalHours = panel.PreviewTotalHours;
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>A plant's growth time is worked out from its config and fertilizer in one place, which every growth tick reads.</summary>
internal static class PlantGrowthPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up plant growth"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "plant growth", speed, () =>
            harmony.Patch(Machine.Method(typeof(PlantComponent), "CalcActualGrowthTime"), postfix: Machine.Patch(typeof(PlantGrowthPatch), nameof(Scale))));
    }

    private static void Scale(ref float __result)
    {
        try
        {
            var gameTime = __result;

            var time = Scaling.Duration(gameTime, _speed);

            __result = time;
            _first.Note(() => $"first plant growth time: game {gameTime:0.##}, now {time:0.##}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// A fuel generator takes fuel as it runs; taking more per stretch makes the tank run dry sooner. The panel's time
/// left is divided by the same speed so it matches.
/// </summary>
internal static class FuelBurnPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "changing the fuel burn"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "fuel burn", speed,
            () => harmony.Patch(Machine.Method(typeof(GeneratorFuelComponent), nameof(GeneratorFuelComponent.ConsumeFuel)), prefix: Machine.Patch(typeof(FuelBurnPatch), nameof(ScaleBurn))),
            () => harmony.Patch(Machine.Method(typeof(GeneratorFuelComponent), nameof(GeneratorFuelComponent.GetRemainingGameSeconds)), postfix: Machine.Patch(typeof(FuelBurnPatch), nameof(ScaleTimeLeft))));
    }

    private static void ScaleBurn(ref float burnValueToConsume)
    {
        try
        {
            var gameBurn = burnValueToConsume;

            var burn = Scaling.Burn(gameBurn, _speed);

            burnValueToConsume = burn;
            _first.Note(() => $"first fuel burn: game {gameBurn:0.###}, now {burn:0.###}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void ScaleTimeLeft(ref float __result)
    {
        try
        {
            __result = Scaling.Duration(__result, _speed);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>A drone trip is given its return time, in game seconds, when it departs.</summary>
internal static class DroneTripPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up a drone trip"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "drone trips", speed, () =>
            harmony.Patch(Machine.Method(typeof(DroneTripComponent), nameof(DroneTripComponent.StartTrip)), prefix: Machine.Patch(typeof(DroneTripPatch), nameof(Scale))));
    }

    private static void Scale(int departSec, ref int returnSec)
    {
        try
        {
            var gameReturn = returnSec;

            var back = Scaling.ReturnTime(departSec, gameReturn, _speed);

            returnSec = back;
            _first.Note(() => $"first drone trip: game {gameReturn - departSec}s, now {back - departSec}s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// A cook is handed its length in game seconds when it starts; fuel burns by progress, so a shorter cook burns the
/// same fuel per dish.
/// </summary>
internal static class CookingPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up a cook"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "cooking", speed, () =>
            harmony.Patch(Machine.Method(typeof(CookingComponent), nameof(CookingComponent.StartCooking)), prefix: Machine.Patch(typeof(CookingPatch), nameof(Scale))));
    }

    private static void Scale(ref float cookDuration)
    {
        try
        {
            var gameDuration = cookDuration;

            var duration = Scaling.Duration(gameDuration, _speed);

            cookDuration = duration;
            _first.Note(() => $"first cook: game {gameDuration:0.##}s, now {duration:0.##}s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
