using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineSpeed.Core;

/// <summary>The machines the Machines window lists, in the order its rows are grouped.</summary>
public enum MachineKind
{
    Shredder,
    RationPress,
    DyeVat,
    Brewing,
    Cooking,
    Drone,
    Generator,
    Compost,
    Plants,
}

/// <summary>Where a machine is. Finished means its product, or its drone, is waiting for the player.</summary>
public enum MachineState
{
    Running,
    Paused,
    Idle,
    Finished,
    OutOfFuel,
}

/// <summary>The window's sections, in the order they are listed.</summary>
public enum MachineGroup
{
    Needs,
    Working,
    Idle,
}

/// <summary>What a machine holds or makes, for the window's second line.</summary>
public abstract record Contents
{
    private Contents()
    {
    }

    /// <summary>The game's own name for the product.</summary>
    public sealed record Named(string Name) : Contents;

    /// <summary>A compost bin's items.</summary>
    public sealed record Items(int Count) : Contents;

    /// <summary>Every pot at home, as one row.</summary>
    public sealed record Plants(int Growing, int Ready) : Contents;
}

/// <summary>
/// One machine as the window shows it. RemainSeconds is game seconds until it is done (until its fuel runs out for a
/// generator), already at the machine's speed; null when the game does not time it. CanRush is the game side's say
/// that this machine has a safe way to be finished now. TotalSeconds is the whole job at the same speed, for the
/// progress bar; null when the game does not say. Icon is the game's web icon for the machine, relative to the WebUI
/// folder; empty when it has none.
/// </summary>
public sealed record MachineRow(MachineKind Kind, long Id, string Name, Contents? Contents, MachineState State, double? RemainSeconds, bool CanRush, double? TotalSeconds = null, string Icon = "")
{
    /// <summary>How much of the job is done, 0 to 1; null when the row is not running a job of known length.</summary>
    public double? Progress => (State is MachineState.Running or MachineState.Paused) && RemainSeconds is { } remain && TotalSeconds is { } total && total > 0 && double.IsFinite(remain) && double.IsFinite(total)
        ? Math.Clamp(1 - remain / total, 0, 1)
        : null;
}

/// <summary>What one read of the home found.</summary>
public abstract record MachineRead
{
    private MachineRead()
    {
    }

    /// <summary>No game is loaded, or it is still loading.</summary>
    public sealed record NoGame : MachineRead;

    /// <summary>The player is away from home; machines are only listed at home.</summary>
    public sealed record Away : MachineRead;

    /// <summary>Points is null when the game's survival points could not be read.</summary>
    public sealed record Home(IReadOnlyList<MachineRow> Rows, int? Points) : MachineRead;
}

public static class MachineKinds
{
    private static readonly (MachineKind Kind, string Key)[] Keys =
    {
        (MachineKind.Shredder, "shredder"),
        (MachineKind.RationPress, "press"),
        (MachineKind.DyeVat, "dye"),
        (MachineKind.Brewing, "brew"),
        (MachineKind.Cooking, "cook"),
        (MachineKind.Drone, "drone"),
        (MachineKind.Generator, "generator"),
        (MachineKind.Compost, "compost"),
        (MachineKind.Plants, "plants"),
    };

    /// <summary>The kind's name in page messages and toast keys.</summary>
    public static string Key(MachineKind kind)
    {
        foreach (var entry in Keys)
        {
            if (entry.Kind == kind) return entry.Key;
        }

        throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
    }

    public static bool TryParse(string key, out MachineKind kind)
    {
        foreach (var entry in Keys)
        {
            if (entry.Key != key) continue;

            kind = entry.Kind;

            return true;
        }

        kind = default;

        return false;
    }
}

public static class MachineList
{
    /// <summary>
    /// The window's section for a row. Only a finished machine or one out of fuel needs the player; an idle one is
    /// simply not in use, so it is listed last and not counted.
    /// </summary>
    public static MachineGroup Group(MachineRow row) => row.State switch
    {
        MachineState.Finished or MachineState.OutOfFuel => MachineGroup.Needs,
        MachineState.Running or MachineState.Paused => MachineGroup.Working,
        _ => MachineGroup.Idle,
    };

    /// <summary>How many rows are in a section: the HUD button shows the Working and Needs counts.</summary>
    public static int Count(IReadOnlyList<MachineRow> rows, MachineGroup group)
    {
        var count = 0;

        for (var i = 0; i < rows.Count; i++)
        {
            if (Group(rows[i]) == group) count++;
        }

        return count;
    }

    /// <summary>
    /// Machines that need the player come first, then the running ones soonest done first, then paused ones, then idle
    /// ones. Ties keep the kind order and then the name, so rows do not jump about between refreshes.
    /// </summary>
    public static IReadOnlyList<MachineRow> Ordered(IEnumerable<MachineRow> rows) =>
        rows.OrderBy(row => Rank(row.State))
            .ThenBy(row => row.State == MachineState.Running ? row.RemainSeconds ?? double.MaxValue : 0)
            .ThenBy(row => row.Kind)
            .ThenBy(row => row.Name, StringComparer.Ordinal)
            .ThenBy(row => row.Id)
            .ToList();

    private static int Rank(MachineState state) => state switch
    {
        MachineState.Finished => 0,
        MachineState.OutOfFuel => 1,
        MachineState.Running => 2,
        MachineState.Paused => 3,
        MachineState.Idle => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
