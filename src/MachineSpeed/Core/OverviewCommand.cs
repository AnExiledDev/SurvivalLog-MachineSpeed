using System;
using System.Globalization;

namespace MachineSpeed.Core;

/// <summary>A message from the Machines window or its HUD button. Anything else the page could send is ignored.</summary>
public abstract record OverviewCommand
{
    public const string Prefix = "slmod|machinespeed|";

    private OverviewCommand()
    {
    }

    public sealed record Toggle : OverviewCommand;

    public sealed record Close : OverviewCommand;

    public sealed record Ready : OverviewCommand;

    public sealed record NoScript : OverviewCommand;

    /// <summary>The machine panels' timer script (page.js) is missing from the web UI, so it must be sent again.</summary>
    public sealed record TimerNoScript : OverviewCommand;

    /// <summary>A diagnostic line from the page script, for the log.</summary>
    public sealed record Note(string Text) : OverviewCommand;

    public sealed record Rush(MachineKind Kind, long Id) : OverviewCommand;

    public static bool IsOurs(string? message) => message != null && message.StartsWith(Prefix, StringComparison.Ordinal);

    public static OverviewCommand? Parse(string? message)
    {
        if (message == null || !IsOurs(message)) return null;

        var body = message.Substring(Prefix.Length);

        if (body.StartsWith("diag|", StringComparison.Ordinal)) return new Note(body.Substring("diag|".Length));

        var parts = body.Split('|');

        return (parts[0], parts.Length) switch
        {
            ("toggle", 1) => new Toggle(),
            ("close", 1) => new Close(),
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            ("timernoscript", 1) => new TimerNoScript(),
            ("rush", 3) when MachineKinds.TryParse(parts[1], out var kind) && long.TryParse(parts[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var id)
                => new Rush(kind, id),
            _ => null,
        };
    }
}
