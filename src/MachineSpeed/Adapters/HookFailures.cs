using System;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>Logs each distinct failure inside one game hook once; a hook that fails on every call stays one line.</summary>
internal sealed class HookFailures
{
    private readonly DistinctFailures _seen = new();
    private readonly Action<string> _log;
    private readonly string _what;
    private readonly string _kept;

    public static HookFailures Silent { get; } = new(_ => { }, "");

    /// <param name="kept">What the game keeps when the hook fails: its duration for a speed, its amount for an amount.</param>
    public HookFailures(Action<string> log, string what, string kept = "duration")
    {
        _log = log;
        _what = what;
        _kept = kept;
    }

    public void Report(Exception ex)
    {
        if (_seen.IsNew($"{ex.GetType().FullName}: {ex.Message}")) _log($"{_what} failed, it keeps the game's {_kept}: {ex}");
    }
}
