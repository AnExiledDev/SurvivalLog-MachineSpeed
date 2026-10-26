using System;
using System.Collections.Generic;

namespace MachineSpeed.Core;

public enum DoneWhat
{
    Finished,
    OutOfFuel,
}

/// <summary>A machine that just finished, or a generator that just ran dry, worth a toast.</summary>
public sealed record DoneNote(MachineRow Row, DoneWhat What);

/// <summary>
/// Remembers each machine's last state between reads, and says which ones just finished. A machine seen for the first
/// time is never news: loading a game or coming home would otherwise toast every finished machine at once. Compost
/// bins and plants have no single moment of being done, so they never toast.
/// </summary>
public sealed class DoneWatch
{
    private readonly Dictionary<(MachineKind Kind, long Id), MachineState> _lastStates = new();

    // Kept between reads and emptied after each, so a read that finds nothing new allocates nothing.
    private readonly HashSet<(MachineKind, long)> _seen = new();
    private readonly List<(MachineKind, long)> _gone = new();

    public IReadOnlyList<DoneNote> See(IReadOnlyList<MachineRow> rows)
    {
        List<DoneNote>? notes = null;

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var key = (row.Kind, row.Id);

            if (!_seen.Add(key)) continue;

            if (_lastStates.TryGetValue(key, out var last) && Note(row, last) is { } what) (notes ??= new List<DoneNote>()).Add(new DoneNote(row, what));

            _lastStates[key] = row.State;
        }

        // A machine that was taken down, or sits on a floor not read this time, starts afresh when it is seen again.
        foreach (var key in _lastStates.Keys)
        {
            if (!_seen.Contains(key)) _gone.Add(key);
        }

        foreach (var key in _gone) _lastStates.Remove(key);

        _gone.Clear();
        _seen.Clear();

        return notes ?? (IReadOnlyList<DoneNote>)Array.Empty<DoneNote>();
    }

    /// <summary>A new game or world: nothing seen before counts any more.</summary>
    public void Forget() => _lastStates.Clear();

    private static DoneWhat? Note(MachineRow row, MachineState last)
    {
        if (row.Kind is MachineKind.Compost or MachineKind.Plants || row.State == last) return null;

        return row.State switch
        {
            MachineState.Finished => DoneWhat.Finished,
            MachineState.OutOfFuel when last is MachineState.Running or MachineState.Paused => DoneWhat.OutOfFuel,
            _ => null,
        };
    }
}
