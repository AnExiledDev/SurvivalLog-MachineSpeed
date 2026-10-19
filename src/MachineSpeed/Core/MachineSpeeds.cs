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
    public const float Fastest = 100000f;

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

/// <summary>
/// Turns the game's machine durations into sped-up ones. A timed job never drops below one game second, so a very
/// high speed finishes a job almost at once but never before it starts; one the game does not time (0 or less) is
/// left alone.
/// </summary>
public static class Scaling
{
    /// <summary>A duration the game keeps as a float, in any unit.</summary>
    public static float Duration(float gameValue, Speed speed) =>
        speed.IsGamePace || !float.IsFinite(gameValue) || gameValue <= 0f ? gameValue : gameValue / speed.Value;

    /// <summary>A job's length in whole game seconds; it never drops below 1.</summary>
    public static int Seconds(int gameSeconds, Speed speed) =>
        speed.IsGamePace || gameSeconds <= 0 ? gameSeconds : Math.Max(1, (int)Math.Round(gameSeconds / (double)speed.Value, MidpointRounding.AwayFromZero));

    /// <summary>
    /// A time a panel shows (minutes, hours or seconds), rounded up so a job still running never reads 0.
    /// </summary>
    public static int Shown(int gameValue, Speed speed) =>
        speed.IsGamePace || gameValue <= 0 ? gameValue : Math.Max(1, (int)Math.Ceiling(gameValue / (double)speed.Value));

    /// <summary>Fuel a generator takes for one stretch of running: a faster burn takes more, so fuel runs out sooner.</summary>
    public static float Burn(float gameBurn, Speed speed) =>
        speed.IsGamePace || !float.IsFinite(gameBurn) || gameBurn <= 0f ? gameBurn : gameBurn * speed.Value;

    /// <summary>
    /// The game burns cooking fuel by how far a cook is times its length, so a faster cook would burn less per dish.
    /// Raising the rate by the cooking speed keeps the fuel per dish the game's, and the fuel amount then sets it.
    /// </summary>
    public static float CookingFuelRate(float gameRate, Speed cooking, FuelUse fuel) =>
        !float.IsFinite(gameRate) || gameRate <= 0f ? gameRate : gameRate * cooking.Value * fuel.Value;

    /// <summary>When a drone gets back, in game seconds; the trip is at least one second long.</summary>
    public static int ReturnTime(int departSec, int gameReturnSec, Speed speed)
    {
        var trip = gameReturnSec - departSec;

        if (speed.IsGamePace || trip <= 0) return gameReturnSec;

        return departSec + Seconds(trip, speed);
    }
}

/// <summary>
/// How much fuel a stove or hot pot uses for the same cooking: 1 is the game's, 0 uses none, 2 uses twice as much.
/// Only <see cref="From"/> makes one.
/// </summary>
public sealed class FuelUse
{
    public const float Most = 20f;

    public static readonly FuelUse GameAmount = new(1f, 1f, SpeedVerdict.AsWritten);

    private FuelUse(float value, float requested, SpeedVerdict verdict)
    {
        Value = value;
        Requested = requested;
        Verdict = verdict;
    }

    public float Value { get; }

    public float Requested { get; }

    public SpeedVerdict Verdict { get; }

    public bool IsGameAmount => Value == 1f;

    /// <summary>Negatives and non-numbers fall back to the game's amount; anything over the most is pulled down.</summary>
    public static FuelUse From(float requested)
    {
        if (!float.IsFinite(requested) || requested < 0f) return new FuelUse(1f, requested, SpeedVerdict.Rejected);

        var value = Math.Min(requested, Most);

        return new FuelUse(value, requested, value == requested ? SpeedVerdict.AsWritten : SpeedVerdict.Clamped);
    }

    public string? Complaint(string key) => Verdict switch
    {
        SpeedVerdict.AsWritten => null,
        SpeedVerdict.Clamped => $"{key} = {Requested} is over {Most}, using {Value}",
        SpeedVerdict.Rejected => $"{key} = {Requested} is not an amount of 0 or more, using the game's (1)",
        _ => throw new ArgumentOutOfRangeException(nameof(Verdict), Verdict, "unknown verdict"),
    };
}

/// <summary>
/// The game times brewing, dyeing and compost in whole game hours and checks them once an hour, so a shorter job
/// cannot be handed to it. Instead the job is moved along: every time this is told the game clock, the extra time a
/// faster speed adds (or a slower one takes away) builds up, and each whole hour of it moves the job one hour on.
/// The part under an hour carries over, which is what lets a job finish between the game's hourly checks.
/// </summary>
public sealed class SpedUpClock
{
    private const double SecondsPerHour = 3600.0;
    private const double MostHoursAtOnce = 1_000_000.0;

    private int? _lastSeconds;
    private double _carrySeconds;

    /// <summary>Game seconds of extra time built up that have not yet made a whole hour; a countdown counts them as done.</summary>
    public double CarrySeconds => _carrySeconds;

    /// <summary>
    /// The whole hours to move the job on since the last call. The first call, and a clock that went back (a save
    /// loaded), only start counting from now.
    /// </summary>
    public int ExtraHours(int nowSeconds, Speed speed)
    {
        if (_lastSeconds is not { } last || nowSeconds < last)
        {
            _lastSeconds = nowSeconds;
            _carrySeconds = 0;

            return 0;
        }

        _lastSeconds = nowSeconds;
        _carrySeconds += (nowSeconds - last) * (speed.Value - 1.0);

        var hours = Math.Clamp(Math.Truncate(_carrySeconds / SecondsPerHour), -MostHoursAtOnce, MostHoursAtOnce);

        _carrySeconds -= hours * SecondsPerHour;

        return (int)hours;
    }

    /// <summary>
    /// How many hours a job of <paramref name="totalHours"/> has now run. It never goes under 0 or past the end; time
    /// cut off at either end is dropped rather than kept for later.
    /// </summary>
    public int Elapsed(int nowSeconds, int elapsedHours, int totalHours, Speed speed)
    {
        var moved = (long)elapsedHours + ExtraHours(nowSeconds, speed);

        if (moved >= 0 && moved <= totalHours) return (int)moved;

        _carrySeconds = 0;

        return (int)Math.Clamp(moved, 0, Math.Max(0, totalHours));
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
