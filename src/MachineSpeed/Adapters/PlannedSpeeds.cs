using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Hands each machine the speed it runs at: its own setting, or the preset's speed when the preset is not Custom,
/// times the day length when following it. A machine's speed is applied again when its setting or the preset changes,
/// and every machine's when the day length does.
/// </summary>
internal sealed class PlannedSpeeds
{
    private readonly ConfigEntry<Preset> _preset;
    private readonly Action<string> _log;
    private readonly List<Action> _applies = new();

    public PlannedSpeeds(ConfigEntry<Preset> preset, Action<string> log)
    {
        _preset = preset;
        _log = log;
    }

    /// <summary>Calls <paramref name="use"/> with the machine's speed now and after each change that moves it.</summary>
    public void Watch(ConfigEntry<float> machine, Action<Speed> use) => Watch(() => use(For(machine)), machine);

    /// <summary>Calls <paramref name="apply"/> now, and after a change of any of the entries, the preset or the day length.</summary>
    public void Watch(Action apply, params ConfigEntryBase[] entries)
    {
        var watched = new ConfigEntryBase[entries.Length + 1];

        entries.CopyTo(watched, 0);
        watched[^1] = _preset;
        _applies.Add(apply);
        OptionalLive.Watch(apply, watched);
    }

    /// <summary>The speed a machine runs at now.</summary>
    public Speed For(ConfigEntry<float> machine) => SpeedPlan.Effective(Own(machine), _preset.Value, DayLengthWorld.Factor);

    /// <summary>A setting's own speed, checked; one not taken as written is logged.</summary>
    public Speed Own(ConfigEntry<float> entry)
    {
        var speed = Speed.From(entry.Value);
        var complaint = speed.Complaint($"{entry.Definition.Section}/{entry.Definition.Key}");

        if (complaint != null) _log(complaint);

        return speed;
    }

    /// <summary>Applies every machine's speed again; one that fails is logged and the rest still apply.</summary>
    public void Reapply()
    {
        foreach (var apply in _applies)
        {
            try
            {
                apply();
            }
            catch (Exception ex)
            {
                _log($"could not apply a machine speed: {ex.Message}");
            }
        }
    }
}
