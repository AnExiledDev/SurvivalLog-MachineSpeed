using System;
using System.IO;
using System.Runtime.CompilerServices;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Shows a "done" toast through Exiled's Toolkit when it is installed. Without it nothing shows. Only
/// <see cref="ShowWithToolkit"/> names a Toolkit type, so the mod loads without it. Clicking a toast opens the
/// Machines window.
/// </summary>
internal sealed class DoneToasts
{
    private readonly Action _openWindow;
    private readonly Action<string> _log;
    private bool _isBroken;
    private bool _hasReportedFailure;

    public DoneToasts(Action openWindow, Action<string> log)
    {
        _openWindow = openWindow;
        _log = log;
    }

    public void Show(DoneNote note, bool isChinese)
    {
        if (_isBroken || !OptionalLive.IsToolkitInstalled) return;

        try
        {
            ShowWithToolkit(note, isChinese, _openWindow);
        }
        catch (Exception ex) when (ex is TypeLoadException or MissingMethodException or FileNotFoundException or FileLoadException)
        {
            _isBroken = true;
            ReportOnce($"Exiled's Toolkit could not show a toast, so finished machines are shown in the Machines window only: {ex.Message}");
        }
        catch (Exception ex)
        {
            ReportOnce($"could not show a finished-machine toast (reported once): {ex.Message}");
        }
    }

    // Kept in its own method so the Toolkit's types are only looked up when it is installed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ShowWithToolkit(DoneNote note, bool isChinese, Action openWindow) =>
        ExiledToolkit.Toasts.Show(new ExiledToolkit.ToastRequest(OverviewTexts.ToastSource.Pick(isChinese), OverviewTexts.ToastTitle(note, isChinese))
        {
            Body = OverviewTexts.ToastBody(note, isChinese),
            Kind = note.What == DoneWhat.OutOfFuel ? ExiledToolkit.ToastKind.Warning : ExiledToolkit.ToastKind.Success,
            Key = "machinespeed|" + OverviewView.Key(note.Row),
            OnClick = openWindow,
        });

    private void ReportOnce(string message)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log(message);
    }
}
