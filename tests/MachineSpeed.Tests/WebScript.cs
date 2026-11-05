using Jint;

namespace MachineSpeed.Tests;

/// <summary>
/// One of Machine Speed's web scripts exactly as the plugin embeds it (overview.js, the Machines window and its HUD
/// button, or page.js, the machine panels' times), run in Jint with Exiled's Toolkit's page API (api.js, from the
/// Toolkit's source) on top of FakePage.js (Root.html, the HUD, the machine panels and the vuplex bridge). The calls into
/// a script are the ones the plugin makes.
/// </summary>
internal sealed class WebScript
{
    public const string Overview = "MachineSpeed.overview.js";

    public const string Panels = "MachineSpeed.page.js";

    private readonly Engine _engine = new();

    /// <param name="script">Which script: <see cref="Overview"/> or <see cref="Panels"/>.</param>
    /// <param name="isToolkitFirst">
    /// The game runs the Toolkit's script first, or with false Machine Speed's first; then the Toolkit's runs only at
    /// <see cref="LoadToolkit"/>.
    /// </param>
    public WebScript(string script = Overview, bool isToolkitFirst = true)
    {
        _engine.Execute(Resource("MachineSpeed.Tests.FakePage.js"));

        if (isToolkitFirst) LoadToolkit();

        _engine.Execute(Resource(script));
    }

    /// <summary>The Toolkit's page API arriving, after Machine Speed's script when the test made it with isToolkitFirst false.</summary>
    public void LoadToolkit() => _engine.Execute(Resource("ExiledToolkit.api.js"));

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    /// <summary>Whether the script posted this message (after "slmod|machinespeed|") to the plugin.</summary>
    public bool HasPosted(string message) => Is($"posted.indexOf('slmod|machinespeed|{message}') >= 0");

    /// <summary>One run of the Toolkit's 250 ms page check.</summary>
    public void Poll() => Run("poll()");

    public void Open() => Run("window.__slmachineoverview.open()");

    public void Close() => Run("window.__slmachineoverview.close()");

    /// <summary>A game loaded or not, with the HUD button's title and tip, as OverviewPage.Live sends it.</summary>
    public void Live(bool isLive) => Run($"window.__slmachineoverview.live({(isLive ? "true" : "false")}, 'Machines', 'Every machine at home')");

    /// <summary>The HUD button's counts, as OverviewPage.Badge sends them.</summary>
    public void Badge(int working, int needs) => Run($"window.__slmachineoverview.badge({working}, {needs})");

    /// <summary>The machine panels' exact times, as TimerPage sends them.</summary>
    public void SetTimes(string json) => Run($"window.__slmachinespeed.set({json})");

    private static string Resource(string name)
    {
        using var stream = typeof(WebScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
