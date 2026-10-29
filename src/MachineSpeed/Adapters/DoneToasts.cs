using System;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Shows Machine Speed's messages as Exiled's Toolkit toasts: a "done" toast, which opens the Machines window when
/// clicked, and a press result while the window is shut.
/// </summary>
internal sealed class DoneToasts
{
    private readonly Action _openWindow;
    private readonly Action<string> _log;

    public DoneToasts(Action openWindow, Action<string> log)
    {
        _openWindow = openWindow;
        _log = log;
    }

    public void Show(DoneNote note, bool isChinese) =>
        Show(new ExiledToolkit.ToastRequest(OverviewTexts.ToastSource.Pick(isChinese), OverviewTexts.ToastTitle(note, isChinese))
        {
            Body = OverviewTexts.ToastBody(note, isChinese),
            Kind = note.What == DoneWhat.OutOfFuel ? ExiledToolkit.ToastKind.Warning : ExiledToolkit.ToastKind.Success,
            Key = "machinespeed|" + OverviewView.Key(note.Row),
            OnClick = _openWindow,
        });

    public void Say(string text, ExiledToolkit.ToastKind kind, bool isChinese)
    {
        if (text.Length > 0) Show(new ExiledToolkit.ToastRequest(OverviewTexts.ToastSource.Pick(isChinese), text) { Kind = kind });
    }

    private void Show(ExiledToolkit.ToastRequest request)
    {
        if (ExiledToolkit.Toasts.Show(request).IsNone) _log("a toast was not shown: " + request.Title);
    }
}
