using System;
using ExiledToolkit.Web;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// The Machines window's half that lives in the game's web UI: overview.js, run in the root document the game's
/// pages sit in. It draws the window and puts the Machines button in the HUD's tool column. The web view can reload at
/// any time and lose the script, so it is checked for every couple of seconds and put back when the page says it is
/// missing.
/// </summary>
internal sealed class OverviewPage
{
    private const string ScriptResource = "MachineSpeed.overview.js";
    private const string Handle = "window.__slmachineoverview";

    private readonly WebPage _page;

    public OverviewPage(Action<string> log)
    {
        _page = new WebPage(typeof(OverviewPage).Assembly, ScriptResource, Handle, OverviewCommand.Prefix + "noscript", log) { Name = "Machines window" };
    }

    public void KeepInstalled(double now) => _page.KeepInstalled(now);

    public void Install() => _page.Install();

    public void Open(string viewJson) => _page.Call($"open({viewJson})");

    public void Show(string viewJson) => _page.Call($"setData({viewJson})");

    public void Close() => _page.Call("close()");

    /// <summary>A result line in the window's footer while it is open. It also frees the buttons.</summary>
    public void Status(string text) => _page.Call($"status({JsText.Literal(text)})");

    /// <summary>Whether a game is loaded (the HUD button shows only then), with the button's tooltip.</summary>
    public bool Live(bool isLive, string title, string tip) => _page.Call($"live({(isLive ? "true" : "false")},{JsText.Literal(title)},{JsText.Literal(tip)})");

    /// <summary>The HUD button's two counts, machines working and machines that need the player, sent whether or not the window is open.</summary>
    public bool Badge(int working, int needs) => _page.Call($"badge({working},{needs})");
}
