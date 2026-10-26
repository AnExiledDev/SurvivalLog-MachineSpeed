using System;
using System.Reflection;
using HarmonyLib;

namespace MachineSpeed.Adapters;

/// <summary>
/// One machine's hooks, put in the first time its speed leaves the game's pace, so a machine never sped up is never
/// hooked and keeps exactly the game's timing. Once in, the hooks stay: at the game's pace every scaling hands back the
/// game's own value. Every hook fails soft: a machine it cannot hook keeps the game's pace.
/// </summary>
internal sealed class MachineHook
{
    private enum HookState
    {
        Waiting,
        In,
        Failed,
    }

    private readonly SpeedLog _log;
    private readonly string _name;
    private readonly Action _install;
    private readonly Action? _installDisplay;
    private HookState _state = HookState.Waiting;

    public MachineHook(SpeedLog log, string name, Action install, Action? installDisplay = null)
    {
        _log = log;
        _name = name;
        _install = install;
        _installDisplay = installDisplay;
    }

    /// <summary>True once the machine's hooks are in, so its jobs follow its speed (the game's pace included).</summary>
    public bool IsIn => _state == HookState.In;

    /// <summary>Called on the main thread at start and after each change of the machine's speed.</summary>
    public void Use(Core.Speed speed)
    {
        if (speed.IsGamePace)
        {
            _log.Write($"{_name} left at the game's pace");

            return;
        }

        if (_state == HookState.Waiting)
        {
            Install(speed);

            return;
        }

        _log.Write(_state == HookState.In ? $"{_name} x{speed.Value:0.##}" : $"{_name} stays at the game's pace, its hook could not go in");
    }

    /// <summary>The display hook only keeps a panel's estimate in step, so losing it is logged and the machine stays sped up.</summary>
    private void Install(Core.Speed speed)
    {
        try
        {
            _install();
        }
        catch (Exception ex)
        {
            _state = HookState.Failed;
            _log.Write($"could not hook {_name}, it stays at the game's pace: {ex.Message}");

            return;
        }

        _state = HookState.In;

        try
        {
            _installDisplay?.Invoke();
            _log.Write($"{_name} x{speed.Value:0.##}");
        }
        catch (Exception ex)
        {
            _log.Write($"{_name} x{speed.Value:0.##}, its panel still shows the game's time ({ex.Message})");
        }
    }
}

internal static class Machine
{
    public static MethodInfo Method(Type type, string name) =>
        AccessTools.Method(type, name) ?? throw new MissingMethodException($"{type.Name}.{name}");

    public static HarmonyMethod Patch(Type patchClass, string method) => new(patchClass, method);
}

/// <summary>
/// Writes the first sped-up use of a machine to the log file, so a player's log shows the hook ran and what it wrote.
/// Callers check <see cref="IsPending"/> before building the line, so a hook that runs every frame builds no string and
/// no closure once the line is written.
/// </summary>
internal sealed class FirstUse
{
    private readonly Action<string> _logFileOnly;
    private bool _isLogged;

    public FirstUse(Action<string> logFileOnly) => _logFileOnly = logFileOnly;

    public static FirstUse Silent { get; } = new(_ => { });

    public bool IsPending => !_isLogged;

    public void Note(string line)
    {
        if (_isLogged) return;

        _isLogged = true;
        _logFileOnly(line);
    }
}
