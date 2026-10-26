using System;
using GameCore.HotUpdate.Battle.Show;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using UnityEngine;

namespace MachineSpeed.Adapters;

/// <summary>
/// While the Machines window is open: stops the game's hotkeys, so a key pressed over the window does not also open a
/// menu, and sends every click to the web UI. The game only passes a click to the web UI where one of its own pages
/// reported a button, which the window is not. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class OverviewInputGate
{
    private static bool _isWindowOpen;
    private static int _blockedThroughFrame = -1;
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;
        Patch(harmony, typeof(HotKeyManager), "Update", prefix: nameof(SkipWhileBlocked), done: "hotkeys are blocked while the Machines window is open", consequence: "keys pressed over the Machines window will also reach the game");
        Patch(harmony, typeof(WebUILayer), "ValidateRaycast", postfix: nameof(RouteClicksToWindow), done: "clicks go to the Machines window while it is open", consequence: "clicks on the Machines window may fall through to the game");
    }

    public static void WindowOpened() => _isWindowOpen = true;

    /// <summary>Keeps hotkeys blocked through the next frame so the key that closed the window (Esc) is not seen by the game too.</summary>
    public static void WindowClosed()
    {
        _isWindowOpen = false;
        _blockedThroughFrame = Time.frameCount + 1;
    }

    private static void Patch(Harmony harmony, Type type, string method, string? prefix = null, string? postfix = null, string done = "", string consequence = "")
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(type, method),
                prefix: prefix == null ? null : new HarmonyMethod(typeof(OverviewInputGate), prefix),
                postfix: postfix == null ? null : new HarmonyMethod(typeof(OverviewInputGate), postfix));
            _log(done);
        }
        catch (Exception ex)
        {
            _log($"could not hook {type.Name}.{method}, {consequence}: {ex.Message}");
        }
    }

    // Runs every frame, so the frame count is read only in the frame or two after the window closes.
    private static bool SkipWhileBlocked()
    {
        if (_isWindowOpen) return false;
        if (_blockedThroughFrame < 0) return true;
        if (Time.frameCount <= _blockedThroughFrame) return false;

        _blockedThroughFrame = -1;

        return true;
    }

    private static void RouteClicksToWindow(ref bool __result)
    {
        if (_isWindowOpen) __result = true;
    }
}
