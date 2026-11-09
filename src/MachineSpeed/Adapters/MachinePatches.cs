using System;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

// One small class per machine because Harmony patches are static and each machine keeps its own speed. Every patch
// runs inside the game's call, so nothing may escape it.

/// <summary>
/// The shredder hands each item's job its length in game seconds when it starts. The panel's time for the next item
/// is the game's minutes, so it is brought in step; the running job's own seconds are already sped up.
/// </summary>
internal static class ShredderPatch
{
    private static Speed _speed = Speed.GamePace;
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up the shredder, it keeps the game's duration", ex);
        _first = new FirstUse(log.WriteFileOnly);
        _hook = new MachineHook(log, "shredder",
            () => hooks.PatchOrThrow(typeof(ShredderPatch), typeof(ShredderComponent), nameof(ShredderComponent.BeginShred), prefix: nameof(ScaleJob)),
            () => hooks.PatchOrThrow(typeof(ShredderPatch), typeof(Furniture), nameof(Furniture.BuildShredderPanelData), postfix: nameof(ScalePanel)));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _hook?.Use(speed);
    }

    private static void ScaleJob(ref int totalSeconds)
    {
        try
        {
            var gameSeconds = totalSeconds;

            var seconds = Scaling.Seconds(gameSeconds, _speed);

            totalSeconds = seconds;
            if (_first.IsPending) _first.Note($"first shred: game {gameSeconds}s, now {seconds}s");
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }

    public static Speed Speed => _speed;

    private static void ScalePanel(ShredderPanelData __result)
    {
        try
        {
            if (__result == null) return;

            __result.TotalSeconds = ShredderTimes.IdleSeconds(__result.TotalSeconds, __result.TimeCost, _speed);
            __result.TimeCost = Scaling.Shown(__result.TimeCost, _speed);
            TimerPage.Changed();
        }
        catch (Exception ex)
        {
            _fail(ex);
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
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up the ration press, it keeps the game's duration", ex);
        _first = new FirstUse(log.WriteFileOnly);
        _hook = new MachineHook(log, "ration press",
            () => hooks.PatchOrThrow(typeof(RationPressPatch), typeof(CompressorComponent), nameof(CompressorComponent.BeginCompress), prefix: nameof(ScaleJob)),
            () => hooks.PatchOrThrow(typeof(RationPressPatch), typeof(Furniture), nameof(Furniture.BuildCompressorPanelData), postfix: nameof(ScalePanel)));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _hook?.Use(speed);
    }

    private static void ScaleJob(ref int totalSeconds)
    {
        try
        {
            var gameSeconds = totalSeconds;

            var seconds = Scaling.Seconds(gameSeconds, _speed);

            totalSeconds = seconds;
            if (_first.IsPending) _first.Note($"first ration batch: game {gameSeconds}s, now {seconds}s");
        }
        catch (Exception ex)
        {
            _fail(ex);
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
            _fail(ex);
        }
    }
}

/// <summary>
/// A plant's growth time is worked out from its config and fertilizer in one place, which every growth tick reads.
/// The planting panel works out its own estimate, so that is brought in step too.
/// </summary>
internal static class PlantGrowthPatch
{
    private static Speed _speed = Speed.GamePace;
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up plant growth, it keeps the game's duration", ex);
        _first = new FirstUse(log.WriteFileOnly);
        PlantCaches.UseLog(log);
        _hook = new MachineHook(log, "plant growth",
            () => hooks.PatchOrThrow(typeof(PlantGrowthPatch), typeof(PlantComponent), "CalcActualGrowthTime", postfix: nameof(Scale)),
            () => hooks.PatchOrThrow(typeof(PlantGrowthPatch), typeof(Reducer_Web_PlantPanel), "RecalcSelection", postfix: nameof(ScaleEstimate)));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _hook?.Use(speed);
    }

    public static Speed Speed => _speed;

    /// <summary>Plants only follow a speed once the growth hook is in.</summary>
    public static bool IsIn => _hook?.IsIn == true;

    // The panel only writes its estimate when the picked seed is in its list; otherwise the value is the one this
    // wrote last time and must not be divided again.
    private static void ScaleEstimate(State_Web_PlantPanel state, int seedIdx)
    {
        try
        {
            var estimate = state?.ExpectedGrowthSeconds;
            var seeds = state?.Seeds;

            if (estimate == null || seeds == null || seedIdx < 0 || seedIdx >= seeds.Count) return;

            estimate.Value = Scaling.Seconds(estimate.Value, _speed);
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }

    private static void Scale(ref float __result)
    {
        try
        {
            var gameTime = __result;

            var time = Scaling.Duration(gameTime, _speed);

            __result = time;
            if (_first.IsPending) _first.Note($"first plant growth time: game {gameTime:0.##}, now {time:0.##}");
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }
}

/// <summary>A drone trip is given its return time, in game seconds, when it departs.</summary>
internal static class DroneTripPatch
{
    private static Speed _speed = Speed.GamePace;
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up a drone trip, it keeps the game's duration", ex);
        _first = new FirstUse(log.WriteFileOnly);
        _hook = new MachineHook(log, "drone trips", () =>
            hooks.PatchOrThrow(typeof(DroneTripPatch), typeof(DroneTripComponent), nameof(DroneTripComponent.StartTrip), prefix: nameof(Scale)));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _hook?.Use(speed);
    }

    private static void Scale(int departSec, ref int returnSec)
    {
        try
        {
            var gameReturn = returnSec;

            var back = Scaling.ReturnTime(departSec, gameReturn, _speed);

            returnSec = back;
            if (_first.IsPending) _first.Note($"first drone trip: game {gameReturn - departSec}s, now {back - departSec}s");
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }
}

/// <summary>A cook is handed its length in game seconds when it starts.</summary>
internal static class CookingPatch
{
    private static Speed _speed = Speed.GamePace;
    private static CookSpeeds? _cooks;
    private static MachineHook? _hook;
    private static Action<Exception> _fail = _ => { };
    private static FirstUse _first = FirstUse.Silent;

    public static void Apply(PatchKit hooks, ModLog log)
    {
        _fail = ex => hooks.Fail("speeding up a cook, it keeps the game's duration", ex);
        _first = new FirstUse(log.WriteFileOnly);
        _hook = new MachineHook(log, "cooking", () =>
            hooks.PatchOrThrow(typeof(CookingPatch), typeof(CookingComponent), nameof(CookingComponent.StartCooking), prefix: nameof(Scale)));
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(Speed speed)
    {
        _speed = speed;
        _cooks ??= new CookSpeeds(speed);
        _hook?.Use(speed);
    }

    /// <summary>
    /// The speed a stove or hot pot's current cook started at. Without the hook every cook keeps the game's length,
    /// so it burns at the game's pace.
    /// </summary>
    public static Speed SpeedOf(CookingComponent cook) =>
        _hook?.IsIn == true ? _cooks?.For(cook.Pointer.ToInt64()) ?? _speed : Speed.GamePace;

    /// <summary>Another world was loaded; its stoves can reuse the old world's addresses.</summary>
    public static void Forget() => _cooks?.Forget();

    private static void Scale(CookingComponent __instance, ref float cookDuration)
    {
        try
        {
            var gameDuration = cookDuration;

            var duration = Scaling.Duration(gameDuration, _speed);

            cookDuration = duration;
            _cooks?.Started(__instance.Pointer.ToInt64(), _speed);
            if (_first.IsPending) _first.Note($"first cook: game {gameDuration:0.##}s, now {duration:0.##}s");
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }
}

/// <summary>
/// A cook burns fuel by how far along it is times its length, so a shorter cook would burn less. The fuel rate is
/// raised by the speed the cook started at to undo that, then set by the player's fuel amount.
/// </summary>
internal static class CookingFuelPatch
{
    private static FuelUse _fuel = FuelUse.GameAmount;
    private static Action<Exception> _fail = _ => { };
    private static FirstUse _first = FirstUse.Silent;
    private static PatchKit? _hooks;
    private static ModLog? _log;
    private static bool _isHooked;
    private static bool _hasFailed;

    public static void Apply(PatchKit hooks, ModLog log)
    {
        (_hooks, _log) = (hooks, log);
        _fail = ex => hooks.Fail("changing the cooking fuel, it keeps the game's duration", ex);
        _first = new FirstUse(log.WriteFileOnly);
    }

    /// <summary>
    /// Called on the main thread at start and after each change of either setting. Like the machines, the hook goes in
    /// the first time it changes anything, and at the game's pace and amount it hands back the game's rate.
    /// </summary>
    public static void Use(Speed cooking, FuelUse fuel)
    {
        _fuel = fuel;

        if (_hooks == null || _log == null) return;

        if (cooking.IsGamePace && fuel.IsGameAmount)
        {
            _log.Write("cooking fuel left at the game's amount");

            return;
        }

        if (_hasFailed) return;

        if (!_isHooked) Hook(_hooks);

        if (_isHooked) _log.Write($"cooking fuel x{fuel.Value:0.##} per dish");
    }

    private static void Hook(PatchKit hooks)
    {
        _isHooked = hooks.Patch(typeof(CookingFuelPatch), typeof(CookingComponent), nameof(CookingComponent.UpdateFuelFromProgress), prefix: nameof(ScaleRate),
            consequence: "a cook burns the game's fuel for its length");
        _hasFailed = !_isHooked;
    }

    private static void ScaleRate(CookingComponent __instance, ref float fuelRate)
    {
        try
        {
            var gameRate = fuelRate;

            var rate = Scaling.CookingFuelRate(gameRate, CookingPatch.SpeedOf(__instance), _fuel);

            fuelRate = rate;
            if (_first.IsPending) _first.Note($"first cooking fuel: game rate {gameRate:0.###}, now {rate:0.###}");
        }
        catch (Exception ex)
        {
            _fail(ex);
        }
    }
}
