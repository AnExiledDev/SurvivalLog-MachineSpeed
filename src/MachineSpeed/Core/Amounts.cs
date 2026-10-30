using System;
using System.Collections.Generic;

namespace MachineSpeed.Core;

/// <summary>
/// How much a machine makes, holds or uses compared with the game: 1 is the game's amount, 2 is twice as much. Only
/// <see cref="From"/> makes one, so a multiplier is never negative, never a non-number and never out of its range.
/// </summary>
public sealed class Multiplier
{
    public static readonly Multiplier GameAmount = new(1f, 1f, SpeedVerdict.AsWritten, 1f, 1f);

    private Multiplier(float value, float requested, SpeedVerdict verdict, float least, float most)
    {
        Value = value;
        Requested = requested;
        Verdict = verdict;
        Least = least;
        Most = most;
    }

    public float Value { get; }

    public float Requested { get; }

    public SpeedVerdict Verdict { get; }

    public float Least { get; }

    public float Most { get; }

    public bool IsGameAmount => Value == 1f;

    /// <summary>
    /// Negatives and non-numbers fall back to the game's amount, and so does 0 when the range starts above it; any
    /// other value is pulled into the range.
    /// </summary>
    public static Multiplier From(float requested, float least, float most)
    {
        var isUnusable = !float.IsFinite(requested) || requested < 0f || requested == 0f && least > 0f;

        if (isUnusable) return new Multiplier(1f, requested, SpeedVerdict.Rejected, least, most);

        var value = Math.Clamp(requested, least, most);

        return new Multiplier(value, requested, value == requested ? SpeedVerdict.AsWritten : SpeedVerdict.Clamped, least, most);
    }

    /// <summary>One line for the log when a setting was not taken as written; null when it was.</summary>
    public string? Complaint(string key) => Verdict switch
    {
        SpeedVerdict.AsWritten => null,
        SpeedVerdict.Clamped => $"{key} = {Requested} is outside {Least} to {Most}, using {Value}",
        SpeedVerdict.Rejected => $"{key} = {Requested} is not a usable amount, using the game's (1)",
        _ => throw new ArgumentOutOfRangeException(nameof(Verdict), Verdict, "unknown verdict"),
    };
}

/// <summary>
/// Turns whole counts into multiplied whole counts without losing the fractions: the part under one is kept per key
/// and added to the next count, so 1.5x on single items gives 1, 2, 1, 2 and never drifts from the exact total.
/// </summary>
public sealed class RemainderCarry<TKey> where TKey : notnull
{
    // A product such as 3 x 1.1 lands a hair under 3.3; this keeps it from losing a whole item to rounding.
    private const double Tolerance = 1e-6;

    private readonly Dictionary<TKey, double> _carry = new();

    /// <summary>What <see cref="Take"/> would give now, without using up the carried fraction.</summary>
    public int Peek(TKey key, int count, Multiplier multiplier) => Split(key, count, multiplier).Whole;

    /// <summary>The multiplied count; the fraction left over is kept for the next count of the same key.</summary>
    public int Take(TKey key, int count, Multiplier multiplier)
    {
        var (whole, fraction) = Split(key, count, multiplier);

        if (count > 0 && !multiplier.IsGameAmount) _carry[key] = fraction;

        return whole;
    }

    private (int Whole, double Fraction) Split(TKey key, int count, Multiplier multiplier)
    {
        if (count <= 0 || multiplier.IsGameAmount) return (count, 0);

        var exact = count * (double)multiplier.Value + _carry.GetValueOrDefault(key);
        var whole = Math.Floor(exact + Tolerance);

        return ((int)Math.Min(whole, int.MaxValue), Math.Max(0, exact - whole));
    }
}

/// <summary>
/// Spreads a new total over the same parts in the same proportions. Each running total is rounded rather than each
/// part, so the parts always add up to the new total and none of them goes negative.
/// </summary>
public static class Shares
{
    /// <summary>
    /// <paramref name="parts"/> were counted against <paramref name="oldTotal"/>; the result is counted against
    /// <paramref name="newTotal"/>. Parts that did not add up to the old total keep the same shortfall in proportion.
    /// </summary>
    public static int[] Rescale(IReadOnlyList<int> parts, int oldTotal, int newTotal)
    {
        var scaled = new int[parts.Count];

        if (oldTotal <= 0 || newTotal < 0)
        {
            for (var i = 0; i < parts.Count; i++) scaled[i] = parts[i];

            return scaled;
        }

        long runningOld = 0;
        long previousNew = 0;

        for (var i = 0; i < parts.Count; i++)
        {
            runningOld += Math.Max(0, parts[i]);

            var runningNew = (long)Math.Round(runningOld * (double)newTotal / oldTotal, MidpointRounding.AwayFromZero);

            scaled[i] = (int)(runningNew - previousNew);
            previousNew = runningNew;
        }

        return scaled;
    }
}

/// <summary>The game's values with an amount setting applied. A value the game does not use (0 or less) is left alone.</summary>
public static class Amounts
{
    /// <summary>The game treats a sealed cold store as nearly stopping decay at this rate; a fridge never goes below it.</summary>
    public const float ColdestRate = 0.002f;

    /// <summary>A rate such as charging or power output, taken times the multiplier.</summary>
    public static float Rate(float gameRate, Multiplier multiplier) =>
        multiplier.IsGameAmount || !float.IsFinite(gameRate) || gameRate <= 0f ? gameRate : gameRate * multiplier.Value;

    /// <summary>A rate that already has <paramref name="applied"/> in it, moved over to a new multiplier.</summary>
    public static float Reapply(float current, float applied, Multiplier multiplier) =>
        !float.IsFinite(current) || current <= 0f || !float.IsFinite(applied) || applied <= 0f ? current : current / applied * multiplier.Value;

    /// <summary>A capacity in whole units: the multiplied amount rounded, and never under 1.</summary>
    public static float Capacity(float gameCapacity, Multiplier multiplier) =>
        multiplier.IsGameAmount || !float.IsFinite(gameCapacity) || gameCapacity <= 0f
            ? gameCapacity
            : Math.Max(1f, MathF.Round(gameCapacity * multiplier.Value, MidpointRounding.AwayFromZero));

    /// <summary>
    /// How fast food spoils in a fridge, as a share of the normal rate (0.2 is a fifth). A stronger fridge divides it:
    /// strength 2 halves the spoiling. It stays between <see cref="ColdestRate"/> and 1, where a fridge stops helping.
    /// A rate of 1 or more is not a fridge's and is left alone.
    /// </summary>
    public static float ColdRate(float gameRate, Multiplier strength) =>
        strength.IsGameAmount || !float.IsFinite(gameRate) || gameRate <= 0f || gameRate >= 1f
            ? gameRate
            : Math.Clamp(gameRate / strength.Value, ColdestRate, 1f);

    /// <summary>The lowest spoil rate the fridge talent can bring a fridge to, a tenth of the normal rate.</summary>
    public const float TalentFloor = 0.1f;

    /// <summary>
    /// A fridge's spoil rate with the fridge talent, worked out as the game does: the talent's share comes off the
    /// rate, but never below <see cref="TalentFloor"/>.
    /// </summary>
    public static float TalentColdRate(float gameRate, float talentRatio) =>
        gameRate < 1f && talentRatio > 0f ? Math.Max(TalentFloor, Math.Max(0f, 1f - talentRatio) * gameRate) : gameRate;

    /// <summary>
    /// A fridge's spoil rate with the fridge talent first and the strength after it. The game applies the talent last
    /// and stops it at a tenth, which would undo any strength past that once the player has the talent.
    /// </summary>
    public static float FridgeRate(float gameRate, float talentRatio, Multiplier strength) =>
        ColdRate(TalentColdRate(gameRate, float.IsFinite(talentRatio) ? talentRatio : 0f), strength);
}
