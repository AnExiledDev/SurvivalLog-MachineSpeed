using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

// One small class per machine because Harmony patches are static and each machine keeps its own speed. Every patch
// runs inside the game's own call, so nothing may escape it.

/// <summary>
/// The shredder hands each item's job its length in game seconds when it starts. The panel's time for the next item
/// is the game's minutes, so it is brought in step; the running job's own seconds are already sped up.
/// </summary>
internal static class ShredderPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up the shredder"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "shredder", speed,
            () => harmony.Patch(Machine.Method(typeof(ShredderComponent), nameof(ShredderComponent.BeginShred)), prefix: Machine.Patch(typeof(ShredderPatch), nameof(ScaleJob))),
            () => harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildShredderPanelData)), postfix: Machine.Patch(typeof(ShredderPatch), nameof(ScalePanel))));
    }

    private static void ScaleJob(ref int totalSeconds)
    {
        try
        {
            var gameSeconds = totalSeconds;

            var seconds = Scaling.Seconds(gameSeconds, _speed);

            totalSeconds = seconds;
            _first.Note(() => $"first shred: game {gameSeconds}s, now {seconds}s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void ScalePanel(ShredderPanelData __result)
    {
        try
        {
            if (__result != null) __result.TimeCost = Scaling.Shown(__result.TimeCost, _speed);
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}

/// <summary>
/// The Ration Press hands a batch its length in game seconds when it starts. The panel's minutes for a batch are the
/// game's, so they are brought in step; the running batch's own seconds are already sped up.
/// </summary>
internal static class RationPressPatch
{
    private static Speed _speed = Speed.GamePace;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed speed, SpeedLog log)
    {
        (_speed, _failures, _first) = (speed, new HookFailures(log.Write, "speeding up the ration press"), new FirstUse(log.WriteFileOnly));
        Machine.Hook(log, "ration press", speed,
            () => harmony.Patch(Machine.Method(typeof(CompressorComponent), nameof(CompressorComponent.BeginCompress)), prefix: Machine.Patch(typeof(RationPressPatch), nameof(ScaleJob))),
            () => harmony.Patch(Machine.Method(typeof(Furniture), nameof(Furniture.BuildCompressorPanelData)), postfix: Machine.Patch(typeof(RationPressPatch), nameof(ScalePanel))));
    }

    private static void ScaleJob(ref int totalSeconds)
    {
        try
        {
            var gameSeconds = totalSeconds;

            var seconds = Scaling.Seconds(gameSeconds, _speed);

            totalSeconds = seconds;
            _first.Note(() => $"first ration batch: game {gameSeconds}s, now {seconds}s");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }

    private static void ScalePanel(CompressorPanelData __result)
    {
        try
        {
            if (__result != null) __result.Minutes = Scaling.Shown(__result.Minutes, _speed);
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

/// <summary>A cook is handed its length in game seconds when it starts.</summary>
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

/// <summary>
/// A cook burns fuel by how far along it is times its length, so a shorter cook would burn less. The fuel rate is
/// raised by the cooking speed to undo that, then set by the player's fuel amount.
/// </summary>
internal static class CookingFuelPatch
{
    private static Speed _cooking = Speed.GamePace;
    private static FuelUse _fuel = FuelUse.GameAmount;
    private static HookFailures _failures = HookFailures.Silent;
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(Harmony harmony, Speed cooking, FuelUse fuel, SpeedLog log)
    {
        (_cooking, _fuel, _failures, _first) = (cooking, fuel, new HookFailures(log.Write, "changing the cooking fuel"), new FirstUse(log.WriteFileOnly));

        if (cooking.IsGamePace && fuel.IsGameAmount)
        {
            log.Write("cooking fuel left at the game's amount");

            return;
        }

        try
        {
            harmony.Patch(Machine.Method(typeof(CookingComponent), nameof(CookingComponent.UpdateFuelFromProgress)), prefix: Machine.Patch(typeof(CookingFuelPatch), nameof(ScaleRate)));
            log.Write($"cooking fuel x{fuel.Value:0.##} per dish");
        }
        catch (Exception ex)
        {
            log.Write($"could not hook the cooking fuel, a cook burns the game's fuel for its length: {ex.Message}");
        }
    }

    private static void ScaleRate(ref float fuelRate)
    {
        try
        {
            var gameRate = fuelRate;

            var rate = Scaling.CookingFuelRate(gameRate, _cooking, _fuel);

            fuelRate = rate;
            _first.Note(() => $"first cooking fuel: game rate {gameRate:0.###}, now {rate:0.###}");
        }
        catch (Exception ex)
        {
            _failures.Report(ex);
        }
    }
}
