using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MachineSpeed.Core;

/// <summary>How the window is to be drawn: the language, the Rush price and whether times are game-clock finish times.</summary>
public readonly record struct ViewSettings(bool IsChinese, double RushPointsPerHour, bool IsFinishClock, long NowSeconds);

/// <summary>
/// The Machines window as page.js draws it, written as JSON. The page keeps no rules of its own: every label, time and
/// whether a Rush button is pressable is decided here.
/// </summary>
public static class OverviewView
{
    public static string Write(MachineRead read, ViewSettings settings)
    {
        var json = new StringBuilder("{");
        var isChinese = settings.IsChinese;

        Field(json, "title", OverviewTexts.Title.Pick(isChinese));
        json.Append(',');
        Field(json, "pointsLabel", OverviewTexts.PointsLabel.Pick(isChinese));
        json.Append(',');
        Field(json, "needsTitle", OverviewTexts.NeedsSection.Pick(isChinese));
        json.Append(',');
        Field(json, "workingTitle", OverviewTexts.WorkingSection.Pick(isChinese));
        json.Append(',');
        Field(json, "toClose", OverviewTexts.ToClose.Pick(isChinese));

        switch (read)
        {
            case MachineRead.Home home:
                var waiting = MachineList.WaitingCount(home.Rows);

                json.Append(',');
                Field(json, "atHome", home.Rows.Count == 0 ? "" : OverviewTexts.AtHome.Say(isChinese, home.Rows.Count));
                json.Append(',');
                Field(json, "needYou", waiting == 0 ? "" : OverviewTexts.NeedYou.Say(isChinese, waiting));
                json.Append(',');
                Field(json, "points", home.Points is { } points ? points.ToString(CultureInfo.InvariantCulture) : "");
                json.Append(',');
                Field(json, "note", home.Rows.Count == 0 ? OverviewTexts.Empty.Pick(isChinese) : "");
                json.Append(",\"rows\":[");
                Join(json, MachineList.Ordered(home.Rows), row => Row(json, row, home.Points, settings));
                json.Append(']');
                break;
            default:
                json.Append(",\"atHome\":\"\",\"needYou\":\"\",\"points\":\"\",");
                Field(json, "note", (read is MachineRead.Away ? OverviewTexts.Away : OverviewTexts.NoGame).Pick(isChinese));
                json.Append(",\"rows\":[]");
                break;
        }

        return json.Append('}').ToString();
    }

    /// <summary>The key names one machine across refreshes, so the page can keep its row.</summary>
    public static string Key(MachineRow row) => MachineKinds.Key(row.Kind) + "|" + row.Id.ToString(CultureInfo.InvariantCulture);

    private static void Row(StringBuilder json, MachineRow row, int? points, ViewSettings settings)
    {
        var isChinese = settings.IsChinese;

        json.Append('{');
        Field(json, "key", Key(row));
        json.Append(',');
        Field(json, "kind", MachineKinds.Key(row.Kind));
        json.Append(',');
        Field(json, "id", row.Id.ToString(CultureInfo.InvariantCulture));
        json.Append(',');
        Field(json, "name", OverviewTexts.Name(row, isChinese));
        json.Append(',');
        Field(json, "product", OverviewTexts.Product(row.Contents, isChinese));
        json.Append(',');
        Field(json, "state", row.State.ToString().ToLowerInvariant());
        json.Append(',');
        Field(json, "stateText", OverviewTexts.State(row, isChinese));
        json.Append(',');
        Field(json, "group", MachineList.IsWaiting(row) ? "needs" : "working");
        json.Append(',');
        Field(json, "icon", row.Icon);
        json.Append(',');
        Field(json, "hint", OverviewTexts.Hint(row, isChinese));
        json.Append(",\"progress\":");
        json.Append(row.Progress is { } progress ? progress.ToString("0.###", CultureInfo.InvariantCulture) : "null");
        json.Append(",\"time\":");
        Clock(json, OverviewTexts.Clock(row, settings.NowSeconds, settings.IsFinishClock, isChinese));
        json.Append(",\"rush\":");
        Rush(json, row, points, settings);
        json.Append('}');
    }

    // Unread points hide the button rather than show a price the player cannot weigh.
    private static void Rush(StringBuilder json, MachineRow row, int? points, ViewSettings settings)
    {
        var isChinese = settings.IsChinese;
        var plan = points is { } have ? RushCost.Plan(row, have, settings.RushPointsPerHour) : new RushPlan.NotRushable();

        switch (plan)
        {
            case RushPlan.Affordable affordable:
                RushButton(json, OverviewTexts.RushLabel.Pick(isChinese), affordable.Cost, isPressable: true, OverviewTexts.RushTip.Say(isChinese, affordable.Cost), "");
                break;
            case RushPlan.TooFew tooFew:
                RushButton(json, OverviewTexts.RushLabel.Pick(isChinese), tooFew.Cost, isPressable: false, OverviewTexts.RushTooFewTip.Say(isChinese, tooFew.Cost, tooFew.Points), OverviewTexts.RushTooFewShort.Say(isChinese, tooFew.Cost, tooFew.Points));
                break;
            default:
                json.Append("null");
                break;
        }
    }

    // "why" is the short line under a Rush the player cannot afford; the web view draws no hover tips.
    private static void RushButton(StringBuilder json, string label, int cost, bool isPressable, string tip, string why)
    {
        json.Append('{');
        Field(json, "label", label);
        json.Append(',');
        Field(json, "cost", cost.ToString(CultureInfo.InvariantCulture));
        json.Append(",\"ok\":").Append(isPressable ? "true" : "false").Append(',');
        Field(json, "tip", tip);
        json.Append(',');
        Field(json, "why", why);
        json.Append('}');
    }

    private static void Clock(StringBuilder json, RowClock? clock)
    {
        if (clock is not { } shown)
        {
            json.Append("null");

            return;
        }

        json.Append("{\"main\":");
        Time(json, shown.Main);
        json.Append(",\"other\":");

        if (shown.Other is { } other) Time(json, other);
        else json.Append("null");

        json.Append('}');
    }

    private static void Time(StringBuilder json, TimeText time)
    {
        json.Append('{');
        Field(json, "value", time.Value);
        json.Append(',');
        Field(json, "word", time.Word);
        json.Append('}');
    }

    private static void Field(StringBuilder json, string name, string value) => json.Append('"').Append(name).Append("\":").Append(JsText.Literal(value));

    private static void Join<T>(StringBuilder json, IEnumerable<T> items, Action<T> write)
    {
        var isFirst = true;

        foreach (var item in items)
        {
            if (!isFirst) json.Append(',');

            isFirst = false;
            write(item);
        }
    }
}
