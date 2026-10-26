using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using HarmonyLib;
using MachineSpeed.Core;
using Vuplex.WebView.Internal;

namespace MachineSpeed.Adapters;

/// <summary>
/// Catches the Machines window's and its HUD button's messages before the game's web layer sees them (it would log
/// them as unknown) and queues them for the next frame, so nothing runs on the web view's thread. Static because a
/// Harmony prefix has nowhere else to look.
/// </summary>
internal static class OverviewMessages
{
    private static readonly ConcurrentQueue<OverviewCommand> Queue = new();
    private static Action<string> _log = _ => { };
    private static int _hasReportedFailure;

    /// <summary>Whether the page's messages reach the mod, so a page can be asked what it is missing instead of sent everything.</summary>
    public static bool IsListening { get; private set; }

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            harmony.Patch(AccessTools.Method(typeof(BaseWebView), "HandleMessageEmitted"), prefix: new HarmonyMethod(typeof(OverviewMessages), nameof(TakeOurs)));
            IsListening = true;
            log("listening for Machines window messages");
        }
        catch (Exception ex)
        {
            log("could not hook Machines window messages, its HUD button and Rush buttons will do nothing: " + ex.Message);
        }
    }

    public static bool TryTake([NotNullWhen(true)] out OverviewCommand? command) => Queue.TryDequeue(out command);

    /// <summary>Never throws into the web view: on any failure the message goes on to the game as if the mod were not here.</summary>
    private static bool TakeOurs(string serializedMessage)
    {
        try
        {
            if (!OverviewCommand.IsOurs(serializedMessage)) return true;

            // The machine panels' timer script is not the window's, so its answer goes straight to it.
            var command = OverviewCommand.Parse(serializedMessage);

            if (command is OverviewCommand.TimerNoScript) TimerPage.AskInstall();
            else if (command != null) Queue.Enqueue(command);

            return false;
        }
        catch (Exception ex)
        {
            ReportOnce(ex);

            return true;
        }
    }

    /// <summary>Runs on the web view's thread, so the once-only flag is set atomically.</summary>
    private static void ReportOnce(Exception ex)
    {
        if (Interlocked.Exchange(ref _hasReportedFailure, 1) == 0) _log("reading a web UI message failed, it was passed on to the game (reported once): " + ex);
    }
}
