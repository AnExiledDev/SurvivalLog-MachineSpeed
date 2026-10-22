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

    public IReadOnlyList<DoneNote> See(IEnumerable<MachineRow> rows)
    {
        var notes = new List<DoneNote>();
        var seen = new HashSet<(MachineKind, long)>();

        foreach (var row in rows)
        {
            var key = (row.Kind, row.Id);

            if (!seen.Add(key)) continue;

            if (_lastStates.TryGetValue(key, out var last) && Note(row, last) is { } what) notes.Add(new DoneNote(row, what));

            _lastStates[key] = row.State;
        }

        // A machine that was taken down, or sits on a floor not read this time, starts afresh when it is seen again.
        foreach (var key in new List<(MachineKind, long)>(_lastStates.Keys))
        {
            if (!seen.Contains(key)) _lastStates.Remove(key);
        }

        return notes;
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
