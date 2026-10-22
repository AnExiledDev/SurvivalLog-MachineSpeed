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
/// that this machine has a safe way to be finished now.
/// </summary>
public sealed record MachineRow(MachineKind Kind, long Id, string Name, Contents? Contents, MachineState State, double? RemainSeconds, bool CanRush);

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
    public static string Key(MachineKind kind) => Keys.First(entry => entry.Kind == kind).Key;

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
    /// <summary>The HUD button's count: machines waiting for the player, idle, finished or out of fuel.</summary>
    public static int WaitingCount(IEnumerable<MachineRow> rows) => rows.Count(row => row.State is MachineState.Idle or MachineState.Finished or MachineState.OutOfFuel);

    /// <summary>
    /// Machines waiting for the player come first, then the running ones soonest done first, then paused ones. Ties
    /// keep the kind order and then the name, so rows do not jump about between refreshes.
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
        MachineState.Idle => 2,
        MachineState.Running => 3,
        MachineState.Paused => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
