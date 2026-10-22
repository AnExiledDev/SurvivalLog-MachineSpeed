using System;

namespace MachineSpeed.Core;

/// <summary>One setting that sets every machine's speed at once; Custom keeps each machine's own setting.</summary>
public enum Preset
{
    Custom,
    Vanilla,
    Relaxed,
    Fast,
    Instant,
}

/// <summary>
/// How much longer a game day is in real time than the chapter makes it, read from the time scale the world started
/// with. Machines time their jobs in game seconds, so multiplying their speed by this keeps their pace in real time.
/// Only <see cref="From"/> and <see cref="None"/> make one.
/// </summary>
public sealed class DayFactor
{
    public static readonly DayFactor None = new(1.0);

    private DayFactor(double value) => Value = value;

    public double Value { get; }

    public bool IsNone => Value == 1.0;

    /// <summary>
    /// The chapter's own time scale over the one the world ended up with (game seconds per real second). A scale the
    /// game never set (0 or less) means there is nothing to follow.
    /// </summary>
    public static DayFactor From(int chapterScale, int actualScale) =>
        chapterScale <= 0 || actualScale <= 0 || chapterScale == actualScale ? None : new DayFactor((double)chapterScale / actualScale);
}

/// <summary>Works out the speed a machine runs at from its own setting, the preset and the day length.</summary>
public static class SpeedPlan
{
    /// <summary>A preset not in the list (a hand-edited config) counts as Custom, so each machine keeps its own speed.</summary>
    public static Preset Known(Preset preset) => Enum.IsDefined(preset) ? preset : Preset.Custom;

    /// <summary>The speed a preset gives every machine; null for Custom, which leaves each machine its own.</summary>
    public static float? PresetSpeed(Preset preset) => Known(preset) switch
    {
        Preset.Custom => null,
        Preset.Vanilla => 1f,
        Preset.Relaxed => 2f,
        Preset.Fast => 5f,
        Preset.Instant => Speed.Fastest,
        var other => throw new ArgumentOutOfRangeException(nameof(preset), other, "unknown preset"),
    };

    /// <summary>
    /// The preset's speed in place of the machine's own unless the preset is Custom, then times the day factor. The
    /// result is pulled into the usual range, so a fast preset on long days still stops at the fastest speed.
    /// </summary>
    public static Speed Effective(Speed own, Preset preset, DayFactor day)
    {
        var presetSpeed = PresetSpeed(preset);

        if (presetSpeed is null && day.IsNone) return own;

        return Speed.From((float)((presetSpeed ?? own.Value) * day.Value));
    }
}
