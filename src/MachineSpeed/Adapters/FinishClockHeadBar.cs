using System;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.Battle.Show;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// With the finish clock on, the timer over a growing plant, a brewing barrel or a dye vat shows the game time it is
/// done instead of the time left. The game writes that timer only when its minute count changes, so the finish time
/// is written right after each of those writes and stays until the next one.
/// </summary>
internal static class FinishClockHeadBar
{
    private const int NotWatching = int.MinValue;

    private static bool _isOn;
    private static bool _isPatched;
    private static Harmony? _harmony;
    private static Action<string> _log = _ => { };
    private static readonly DistinctFailures Failures = new();

    /// <summary>
    /// The hook goes in the first time the setting is on, not at start: the game updates these timers for every plant,
    /// barrel and vat, and a player who never turns the finish clock on should not pay for a hook that does nothing.
    /// </summary>
    public static void Apply(Harmony harmony, SpeedLog log)
    {
        _harmony = harmony;
        _log = log.Write;
    }

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(bool isOn)
    {
        _isOn = isOn;

        if (isOn && !_isPatched) Patch();
    }

    private static void Patch()
    {
        _isPatched = true;

        if (_harmony == null) return;

        try
        {
            _harmony.Patch(
                AccessTools.Method(typeof(FurnitureHeadBar), "UpdatePlantTimeText"),
                prefix: new HarmonyMethod(typeof(FinishClockHeadBar), nameof(Before)),
                postfix: new HarmonyMethod(typeof(FinishClockHeadBar), nameof(After)));
            _log("timers over machines can show their finish time");
        }
        catch (Exception ex)
        {
            _log($"could not hook FurnitureHeadBar.UpdatePlantTimeText, timers over machines keep showing time left: {ex.Message}");
        }
    }

    private static void Before(FurnitureHeadBar __instance, out int __state)
    {
        __state = NotWatching;

        try
        {
            if (_isOn) __state = __instance._lastPlantShownMinutes;
        }
        catch (Exception ex)
        {
            Report(ex);
        }
    }

    private static void After(FurnitureHeadBar __instance, int __state)
    {
        try
        {
            if (__state == NotWatching || __instance._lastPlantShownMinutes == __state) return;

            var world = GameSingleton.Existing<BattleLogicWorld>();
            var text = __instance.Text_HpText;

            if (world?._GameTimeManager == null || text == null) return;

            var remain = Math.Max(0, __instance._plantMaxDuring - __instance._plantDuring);

            text.text = OverviewTexts.HeadBarFinish(world._GameTimeManager.GetTotalSeconds(), remain, GameLanguage.IsChinese());
        }
        catch (Exception ex)
        {
            Report(ex);
        }
    }

    private static void Report(Exception ex)
    {
        if (Failures.IsNew($"{ex.GetType().FullName}: {ex.Message}")) _log($"showing a finish time over a machine failed, it shows time left: {ex}");
    }
}
