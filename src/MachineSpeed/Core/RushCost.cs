using System;

namespace MachineSpeed.Core;

/// <summary>What pressing Rush on a row would do.</summary>
public abstract record RushPlan
{
    private RushPlan()
    {
    }

    /// <summary>The machine is not running, or has no safe way to be finished now.</summary>
    public sealed record NotRushable : RushPlan;

    public sealed record Affordable(int Cost) : RushPlan;

    public sealed record TooFew(int Cost, int Points) : RushPlan;
}

/// <summary>What a press of Rush did.</summary>
public abstract record RushOutcome
{
    private RushOutcome()
    {
    }

    public sealed record Rushed(string Name, int Cost) : RushOutcome;

    public sealed record TooFew(int Cost, int Points) : RushOutcome;

    /// <summary>The machine is gone, no longer running, or cannot be finished safely.</summary>
    public sealed record NotRushable : RushOutcome;

    /// <summary>The game's finish threw; nothing was spent.</summary>
    public sealed record Failed : RushOutcome;
}

/// <summary>
/// Rushing a job costs survival points for the game hours it has left: at least 1, and every part of an hour counts
/// as a whole one.
/// </summary>
public static class RushCost
{
    public const double DefaultPointsPerHour = 0.25;
    public const double FewestPointsPerHour = 0.01;
    public const double MostPointsPerHour = 1000;

    private const double SecondsPerHour = 3600.0;
    // A job a hair over a whole number of points (float time left) must not cost one more.
    private const double Slack = 1e-6;

    /// <summary>The setting as used: 0 or less, or not a number, is the default; the rest is kept in range.</summary>
    public static double Rate(double pointsPerHour) =>
        double.IsFinite(pointsPerHour) && pointsPerHour > 0 ? Math.Clamp(pointsPerHour, FewestPointsPerHour, MostPointsPerHour) : DefaultPointsPerHour;

    public static int Of(double remainSeconds, double pointsPerHour)
    {
        var hours = double.IsFinite(remainSeconds) ? Math.Max(0, remainSeconds) / SecondsPerHour : 0;
        var points = Math.Ceiling(hours * Rate(pointsPerHour) - Slack);

        return (int)Math.Clamp(points, 1, int.MaxValue);
    }

    /// <summary>Only a running job the game side can finish, with time still left on it, can be rushed.</summary>
    public static RushPlan Plan(MachineRow row, int points, double pointsPerHour)
    {
        if (!row.CanRush || row.State != MachineState.Running || row.RemainSeconds is not { } remain || !(remain > 0)) return new RushPlan.NotRushable();

        var cost = Of(remain, pointsPerHour);

        return points >= cost ? new RushPlan.Affordable(cost) : new RushPlan.TooFew(cost, points);
    }
}
