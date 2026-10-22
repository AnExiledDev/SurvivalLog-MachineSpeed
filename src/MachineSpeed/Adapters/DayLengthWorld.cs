using System;
using BepInEx.Unity.IL2CPP;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Reads how much the Day Length mod stretched the day. The game sets the chapter's time scale (game seconds per real
/// second) just before the logic world starts, and Day Length divides it at the top of BattleLogicWorld.Initialize, so
/// once Initialize is done the chapter's number over the one left in Toolset.TimeScale is the day length actually in
/// use, after Day Length's rounding. Nothing here names a Day Length type, so the mod loads without it. The hook goes
/// in only when following is on and Day Length is loaded; every read fails soft to no change.
/// </summary>
internal static class DayLengthWorld
{
    public const string DayLengthGuid = "com.dev.survivallog.daylength";

    private static Harmony? _harmony;
    private static SpeedLog? _log;
    private static Action _changed = () => { };
    private static bool _isFollowing;
    private static bool _isHooked;

    /// <summary>The factor every machine speed is multiplied by; no change until following is on and a world has started.</summary>
    public static DayFactor Factor { get; private set; } = DayFactor.None;

    private static bool IsDayLengthInstalled => IL2CPPChainloader.Instance.Plugins.ContainsKey(DayLengthGuid);

    /// <summary><paramref name="changed"/> is called on the main thread whenever the factor changes.</summary>
    public static void Apply(Harmony harmony, SpeedLog log, Action changed) => (_harmony, _log, _changed) = (harmony, log, changed);

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public static void Use(bool follow)
    {
        _isFollowing = follow;

        if (!follow)
        {
            Change(DayFactor.None);

            return;
        }

        if (!IsDayLengthInstalled)
        {
            _log?.Write("FollowDayLength is on, but Day Length is not installed, so machine speeds are not changed");

            return;
        }

        if (!Hook()) return;

        Change(Measure(GameSingleton.Existing<GameSaveManager>()?.gameSave));
    }

    private static bool Hook()
    {
        if (_isHooked || _harmony == null) return _isHooked;

        try
        {
            _harmony.Patch(Machine.Method(typeof(BattleLogicWorld), nameof(BattleLogicWorld.Initialize)), postfix: Machine.Patch(typeof(DayLengthWorld), nameof(AfterWorldStart)));
            _isHooked = true;
        }
        catch (Exception ex)
        {
            _log?.Write($"could not hook the world start, machine speeds do not follow Day Length: {ex.Message}");
        }

        return _isHooked;
    }

    private static void AfterWorldStart(GameSaveData gameSave)
    {
        if (_isFollowing) Change(Measure(gameSave));
    }

    /// <summary>The chapter's own time scale over the one the world ended up with; no change when either cannot be read.</summary>
    private static DayFactor Measure(GameSaveData? save)
    {
        try
        {
            var chapter = save == null ? null : GameSingleton.Existing<ConfigManager>()?.Get_Config_Chapter(save.InitChapterId);

            return chapter == null ? DayFactor.None : DayFactor.From(chapter.TimeScale, Toolset.TimeScale);
        }
        catch (Exception ex)
        {
            _log?.Write($"could not read the day length, machine speeds are not changed: {ex.Message}");

            return DayFactor.None;
        }
    }

    private static void Change(DayFactor factor)
    {
        if (factor.Value == Factor.Value) return;

        Factor = factor;
        _log?.Write(factor.IsNone ? "machine speeds no longer follow the day length" : $"days are {factor.Value:0.##}x as long, machine speeds follow");

        try
        {
            _changed();
        }
        catch (Exception ex)
        {
            _log?.Write($"could not apply the day length to machine speeds: {ex.Message}");
        }
    }
}
