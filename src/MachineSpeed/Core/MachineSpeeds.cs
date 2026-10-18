using System;
using System.Collections.Generic;

namespace MachineSpeed.Core;

/// <summary>How a configured speed was taken.</summary>
public enum SpeedVerdict
{
    AsWritten,
    Clamped,
    Rejected,
}

/// <summary>
/// A machine speed that has been checked, so it is never zero, negative or out of range. Only <see cref="From"/>
/// makes one, which is what lets the scaling below divide by it without a guard.
/// </summary>
public sealed class Speed
{
    public const float Slowest = 0.25f;
    public const float Fastest = 20f;

    public static readonly Speed GamePace = new(1f, 1f, SpeedVerdict.AsWritten);

    private Speed(float value, float requested, SpeedVerdict verdict)
    {
        Value = value;
        Requested = requested;
        Verdict = verdict;
    }

    public float Value { get; }

    public float Requested { get; }

    public SpeedVerdict Verdict { get; }

    public bool IsGamePace => Value == 1f;

    /// <summary>Zero, negatives and non-numbers fall back to the game's pace; anything else is pulled into range.</summary>
    public static Speed From(float requested)
    {
        if (!float.IsFinite(requested) || requested <= 0f) return new Speed(1f, requested, SpeedVerdict.Rejected);

        var value = Math.Clamp(requested, Slowest, Fastest);

        return new Speed(value, requested, value == requested ? SpeedVerdict.AsWritten : SpeedVerdict.Clamped);
    }

    /// <summary>One line for the log when a setting was not taken as written; null when it was.</summary>
    public string? Complaint(string key) => Verdict switch
    {
        SpeedVerdict.AsWritten => null,
        SpeedVerdict.Clamped => $"{key} = {Requested} is outside {Slowest} to {Fastest}, using {Value}",
        SpeedVerdict.Rejected => $"{key} = {Requested} is not a speed above 0, using the game's pace (1)",
        _ => throw new ArgumentOutOfRangeException(nameof(Verdict), Verdict, "unknown verdict"),
    };
}

/// <summary>A dye vat's panel figures before it is shown; <paramref name="State"/> 0 is an idle vat.</summary>
public readonly record struct DyePreview(int State, int BucketCount, int HoursPerBucket, int PreviewTotalHours);

/// <summary>
/// Turns the game's machine durations into sped-up ones. A duration only ever gets shorter or longer, never zero,
/// so no machine finishes the moment it starts; one the game does not time (0 or less) is left alone.
/// </summary>
public static class Scaling
{
    /// <summary>A duration the game keeps as a float, in any unit.</summary>
    public static float Duration(float gameValue, Speed speed) =>
        speed.IsGamePace || !float.IsFinite(gameValue) || gameValue <= 0f ? gameValue : gameValue / speed.Value;

    /// <summary>A duration the game keeps in whole minutes or hours; it never drops below 1.</summary>
    public static int WholeUnits(int gameValue, Speed speed) =>
        speed.IsGamePace || gameValue <= 0 ? gameValue : Math.Max(1, (int)Math.Round(gameValue / speed.Value, MidpointRounding.AwayFromZero));

    /// <summary>Fuel a generator takes for one stretch of running: a faster burn takes more, so fuel runs out sooner.</summary>
    public static float Burn(float gameBurn, Speed speed) =>
        speed.IsGamePace || !float.IsFinite(gameBurn) || gameBurn <= 0f ? gameBurn : gameBurn * speed.Value;

    /// <summary>When a drone gets back, in game seconds; the trip is at least one second long.</summary>
    public static int ReturnTime(int departSec, int gameReturnSec, Speed speed)
    {
        var trip = gameReturnSec - departSec;

        if (speed.IsGamePace || trip <= 0) return gameReturnSec;

        return departSec + Math.Max(1, (int)Math.Round(trip / speed.Value, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// The panel of an idle vat estimates the next dye job from the game's hours per bucket; this gives the hours the
    /// job will really take. A running vat already shows the sped-up hours it was started with, so it is left alone.
    /// </summary>
    public static DyePreview Dye(DyePreview panel, Speed speed)
    {
        if (speed.IsGamePace || panel.State != 0 || panel.PreviewTotalHours <= 0 || panel.HoursPerBucket <= 0) return panel;

        var hours = WholeUnits(panel.HoursPerBucket, speed);

        return panel with { HoursPerBucket = hours, PreviewTotalHours = panel.BucketCount * hours };
    }
}

/// <summary>The brewing and cooking speeds Action Speed used to keep, read from its config file.</summary>
public readonly record struct OldMachineSpeeds(float? Brewing, float? Cooking);

/// <summary>Reads the parts of a BepInEx config file the first start needs. Pure text in, values out.</summary>
public static class ConfigText
{
    public static bool HasSection(string text, string section)
    {
        foreach (var line in Lines(text))
        {
            if (line == $"[{section}]") return true;
        }

        return false;
    }

    /// <summary>Brewing and Cooking from Action Speed's [Speed] section; a missing or unreadable value is null.</summary>
    public static OldMachineSpeeds ReadOldActionSpeed(string text)
    {
        var values = SectionValues(text, "Speed");

        return new OldMachineSpeeds(Number(values, "Brewing"), Number(values, "Cooking"));
    }

    private static Dictionary<string, string> SectionValues(string text, string section)
    {
        var values = new Dictionary<string, string>();
        var isInSection = false;

        foreach (var line in Lines(text))
        {
            if (line.StartsWith('['))
            {
                isInSection = line == $"[{section}]";

                continue;
            }

            var equals = line.IndexOf('=');

            if (!isInSection || line.StartsWith('#') || equals <= 0) continue;

            values[line[..equals].Trim()] = line[(equals + 1)..].Trim();
        }

        return values;
    }

    private static float? Number(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var raw) && float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static IEnumerable<string> Lines(string text)
    {
        foreach (var line in text.Split('\n')) yield return line.Trim();
    }
}

/// <summary>Lets a hook that can fail on every call log each distinct failure once instead of flooding the log.</summary>
public sealed class DistinctFailures
{
    private readonly HashSet<string> _seen = new();

    /// <summary>True the first time this failure is seen.</summary>
    public bool IsNew(string failure) => _seen.Add(failure);
}
