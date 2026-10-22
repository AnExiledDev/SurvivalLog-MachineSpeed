using System;
using System.Collections.Generic;
using UnityEngine;

namespace MachineSpeed.Adapters;

/// <summary>
/// The Unity object that hands frames to the Machines window. Each distinct failure is logged once, never rethrown
/// into the game; a failure that repeats every frame would otherwise flood the log.
/// </summary>
public sealed class OverviewTicker : MonoBehaviour
{
    private const int MaxReportedFailures = 20;

    internal static OverviewSession? Session;
    internal static Action<string> Log = _ => { };
    private static readonly HashSet<string> ReportedFailures = new();

    public OverviewTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        try
        {
            Session?.Update();
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void ReportFailure(Exception ex)
    {
        if (ReportedFailures.Count >= MaxReportedFailures || !ReportedFailures.Add(ex.GetType().FullName + ": " + ex.Message)) return;

        Log("Machines window update failed (each distinct failure reported once): " + ex);

        if (ReportedFailures.Count == MaxReportedFailures) Log($"{MaxReportedFailures} distinct Machines window failures reported; further ones are not logged");
    }
}
