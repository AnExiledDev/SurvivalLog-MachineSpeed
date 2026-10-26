using System;
using System.Collections.Generic;

namespace MachineSpeed.Core;

/// <summary>
/// One read of every piece of furniture at home, done a few pieces at a time so a big base never costs one frame the
/// whole read. Pieces are visited in order, each exactly once, and the rows come out the same however the read was
/// split. Every pot at home becomes one row: how many are growing, how many are ready, and the soonest ready.
/// </summary>
public sealed class HomeScan
{
    private readonly List<MachineRow> _rows = new();
    private int _count;
    private int _next;
    private int _growing;
    private int _ready;
    private int? _soonest;

    public bool IsDone => _next >= _count;

    public void Start(int count)
    {
        _rows.Clear();
        _count = Math.Max(0, count);
        _next = 0;
        _growing = 0;
        _ready = 0;
        _soonest = null;
    }

    /// <summary>
    /// Visits up to <paramref name="most"/> more pieces, stopping early once <paramref name="isSpent"/> says this frame's
    /// share of time is used up; true once every piece has been visited. A piece is counted as visited before it is
    /// read, so one that throws is never read twice.
    /// </summary>
    public bool Advance(int most, Action<int> visit, Func<bool> isSpent)
    {
        for (var done = 0; done < most && _next < _count; done++)
        {
            visit(_next++);

            if (isSpent()) break;
        }

        return IsDone;
    }

    public void Add(MachineRow row) => _rows.Add(row);

    public void AddGrowing(int remainSeconds)
    {
        var remain = Math.Max(0, remainSeconds);

        _growing++;
        _soonest = _soonest is { } soonest ? Math.Min(soonest, remain) : remain;
    }

    public void AddReady() => _ready++;

    /// <summary>A list of its own, with the pots' row last, so the scan can start again while the read is still in use.</summary>
    public IReadOnlyList<MachineRow> Rows()
    {
        var rows = new List<MachineRow>(_rows.Count + 1);

        rows.AddRange(_rows);

        if (_growing + _ready > 0)
        {
            var state = _ready > 0 ? MachineState.Finished : MachineState.Running;

            rows.Add(new MachineRow(MachineKind.Plants, 0, "", new Contents.Plants(_growing, _ready), state, state == MachineState.Running ? _soonest : null, false,
                Icon: MachineIcons.KindIcon(MachineKind.Plants)));
        }

        return rows;
    }
}
