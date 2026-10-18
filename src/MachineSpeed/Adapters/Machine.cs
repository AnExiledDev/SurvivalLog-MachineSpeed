using System;
using System.Reflection;
using HarmonyLib;

namespace MachineSpeed.Adapters;

/// <summary>Installs one machine's hooks and logs how it went. Every hook fails soft: a machine it cannot hook keeps the game's pace.</summary>
internal static class Machine
{
    /// <summary>
    /// At the game's pace nothing is hooked, which is what keeps the default timing exactly the game's. The display
    /// hook only keeps a panel's estimate in step, so losing it is logged and the machine stays sped up.
    /// </summary>
    public static void Hook(SpeedLog log, string name, Core.Speed speed, Action install, Action? installDisplay = null)
    {
        if (speed.IsGamePace)
        {
            log.Write($"{name} left at the game's pace");

            return;
        }

        try
        {
            install();
        }
        catch (Exception ex)
        {
            log.Write($"could not hook {name}, it stays at the game's pace: {ex.Message}");

            return;
        }

        try
        {
            installDisplay?.Invoke();
            log.Write($"{name} x{speed.Value:0.##}");
        }
        catch (Exception ex)
        {
            log.Write($"{name} x{speed.Value:0.##}, its panel still shows the game's time ({ex.Message})");
        }
    }

    public static MethodInfo Method(Type type, string name) =>
        AccessTools.Method(type, name) ?? throw new MissingMethodException($"{type.Name}.{name}");

    public static HarmonyMethod Patch(Type patchClass, string method) => new(patchClass, method);
}

/// <summary>Writes the first sped-up use of a machine to the log file, so a player's log shows the hook ran and what it wrote.</summary>
internal sealed class FirstUse
{
    private readonly Action<string> _logFileOnly;
    private bool _isLogged;

    public FirstUse(Action<string> logFileOnly) => _logFileOnly = logFileOnly;

    public static FirstUse Silent { get; } = new(_ => { });

    public void Note(Func<string> line)
    {
        if (_isLogged) return;

        _isLogged = true;
        _logFileOnly(line());
    }
}
