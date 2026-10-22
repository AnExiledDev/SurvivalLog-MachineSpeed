using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// The Machines window's half that lives in the game's web UI: overview.js, run in the root document the game's own
/// pages sit in. It draws the window and puts the Machines button in the HUD's tool column. The web view can reload at
/// any time and lose the script, so it is checked for every couple of seconds and put back when the page says it is
/// missing.
/// </summary>
internal sealed class OverviewPage
{
    private const float CheckSeconds = 2f;
    private const string ScriptResource = "MachineSpeed.overview.js";
    private const string Handle = "window.__slmachineoverview";
    private const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + OverviewCommand.Prefix + "noscript')";

    private readonly Action<string> _log;
    private string? _script;
    private float _nextCheckAt;
    private bool _hasReportedRunFailure;

    public OverviewPage(Action<string> log)
    {
        _log = log;
    }

    public void KeepInstalled(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;
        Run(Handle + "||" + NoScriptReply);
    }

    public void Install()
    {
        if (Run(Script())) _log("Machines window script sent to the web UI");
    }

    public void Open(string viewJson) => Call($"open({viewJson})");

    public void Show(string viewJson) => Call($"setData({viewJson})");

    public void Close() => Call("close()");

    /// <summary>A result line: in the window's footer while it is open, as a short note otherwise.</summary>
    public void Status(string text) => Call($"status({JsText.Literal(text)})");

    /// <summary>Whether a game is loaded (the HUD button shows only then), with the button's tooltip.</summary>
    public bool Live(bool isLive, string title, string tip) => Call($"live({(isLive ? "true" : "false")},{JsText.Literal(title)},{JsText.Literal(tip)})");

    /// <summary>The count badge on the HUD button, sent whether or not the window is open.</summary>
    public bool Badge(int count) => Call($"badge({count})");

    private bool Call(string expression) => Run($"{Handle}?{Handle}.{expression}:{NoScriptReply}");

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run the Machines window script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(OverviewPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
