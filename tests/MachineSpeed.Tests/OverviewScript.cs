using Jint;

namespace MachineSpeed.Tests;

/// <summary>
/// overview.js exactly as the plugin embeds it, run in Jint on top of FakePage.js (Root.html, the HUD and the vuplex
/// bridge). The calls into the script are the ones the plugin makes through window.__slmachineoverview.
/// </summary>
internal sealed class OverviewScript
{
    private readonly Engine _engine = new();

    public OverviewScript()
    {
        _engine.Execute(Resource("MachineSpeed.Tests.FakePage.js"));
        _engine.Execute(Resource("MachineSpeed.overview.js"));
    }

    public void Run(string statements) => _engine.Execute(statements);

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    public bool Is(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public void Open() => Run("window.__slmachineoverview.open()");

    public void Close() => Run("window.__slmachineoverview.close()");

    private static string Resource(string name)
    {
        using var stream = typeof(OverviewScript).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
