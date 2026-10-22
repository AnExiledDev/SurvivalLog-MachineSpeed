using System;
using System.Globalization;

namespace MachineSpeed.Core;

/// <summary>One piece of on-screen text in the two languages the game ships. {0}, {1} are filled in by Say.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;

    public string Say(bool isChinese, params object[] args) => string.Format(CultureInfo.InvariantCulture, Pick(isChinese), args);
}

/// <summary>The words of the Machines window, its HUD button, the Rush button and the done toasts.</summary>
public static class OverviewTexts
{
    public static readonly Phrase Title = new("Machines", "机器总览");
    public static readonly Phrase ButtonTip = new("Every machine at home: what it is making and when it is done.", "基地里的所有机器：正在制作什么、何时完成。");
    public static readonly Phrase Points = new("Survival points: {0}", "生存点：{0}");
    public static readonly Phrase Away = new("Machines are listed at home only.", "只在基地时列出机器。");
    public static readonly Phrase NoGame = new("Load a game first.", "请先载入游戏。");
    public static readonly Phrase Empty = new("No machines at home yet.", "基地里还没有机器。");
    public static readonly Phrase MachineColumn = new("Machine", "机器");
    public static readonly Phrase MakingColumn = new("Making", "产物");
    public static readonly Phrase StateColumn = new("State", "状态");
    public static readonly Phrase TimeColumn = new("Time", "时间");

    public static readonly Phrase Running = new("Running", "运行中");
    public static readonly Phrase Paused = new("Paused", "已暂停");
    public static readonly Phrase Idle = new("Idle", "闲置");
    public static readonly Phrase Done = new("Done", "完成");
    public static readonly Phrase OutOfFuel = new("Out of fuel", "燃料耗尽");
    public static readonly Phrase DroneBack = new("Back", "成功返回");
    public static readonly Phrase GeneratorOff = new("Off", "已关闭");
    public static readonly Phrase PlantsReady = new("Ready", "可收获");

    public static readonly Phrase Left = new("{0} left", "剩余：{0}");
    public static readonly Phrase DoneAt = new("Done at {0}", "{0} 完成");
    public static readonly Phrase FuelFor = new("Fuel for {0}", "燃料剩余：{0}");
    public static readonly Phrase FuelUntil = new("Fuel until {0}", "燃料可用到 {0}");
    public static readonly Phrase HeadBarAt = new("at {0}", "{0}完成");

    public static readonly Phrase OneItem = new("1 item", "1 件物品");
    public static readonly Phrase ManyItems = new("{0} items", "{0} 件物品");
    public static readonly Phrase PlantCounts = new("{0} growing, {1} ready", "{0} 株生长中，{1} 株可收获");

    public static readonly Phrase RushLabel = new("Rush ({0} pts)", "加速完成：{0} 生存点");
    public static readonly Phrase RushTip = new("Finish now for {0} survival points", "花费 {0} 生存点立即完成");
    public static readonly Phrase RushTooFewTip = new("Needs {0} survival points, you have {1}", "需要生存点：{0}，拥有生存点：{1}");
    public static readonly Phrase Rushed = new("Finished {0} for {1} survival points", "已花费 {1} 生存点完成「{0}」");
    public static readonly Phrase TooFewPoints = new("Not enough survival points", "生存点不足");
    public static readonly Phrase NotRushable = new("This machine cannot be rushed now", "此机器现在无法加速完成");
    public static readonly Phrase RushFailed = new("Rush failed, nothing was spent", "加速完成失败，未花费生存点");

    public static readonly Phrase ToastSource = new("Machine Speed", "机器加速");

    /// <summary>What to call a machine whose own name could not be read.</summary>
    public static string KindName(MachineKind kind, bool isChinese) => (kind switch
    {
        MachineKind.Shredder => new Phrase("Shredder", "粉碎机"),
        MachineKind.RationPress => new Phrase("Ration Press", "口粮压制机"),
        MachineKind.DyeVat => new Phrase("Dye vat", "染料桶"),
        MachineKind.Brewing => new Phrase("Brewing barrel", "酿酒桶"),
        MachineKind.Cooking => new Phrase("Stove", "燃气灶/火锅"),
        MachineKind.Drone => new Phrase("Drone", "无人机"),
        MachineKind.Generator => new Phrase("Fuel generator", "燃油发电机"),
        MachineKind.Compost => new Phrase("Compost bin", "堆肥箱"),
        MachineKind.Plants => new Phrase("Plants", "植物"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    }).Pick(isChinese);

    public static string Name(MachineRow row, bool isChinese) => string.IsNullOrWhiteSpace(row.Name) ? KindName(row.Kind, isChinese) : row.Name;

    /// <summary>The second line of a row: the product's name, a bin's item count, or the plants' counts.</summary>
    public static string Product(Contents? contents, bool isChinese) => contents switch
    {
        null => "",
        Contents.Named named => named.Name,
        Contents.Items { Count: 1 } => OneItem.Pick(isChinese),
        Contents.Items items => ManyItems.Say(isChinese, items.Count),
        Contents.Plants plants => PlantCounts.Say(isChinese, plants.Growing, plants.Ready),
        _ => throw new ArgumentOutOfRangeException(nameof(contents), contents, null),
    };

    public static string State(MachineRow row, bool isChinese) => (row.Kind, row.State) switch
    {
        (MachineKind.Drone, MachineState.Finished) => DroneBack.Pick(isChinese),
        (MachineKind.Generator, MachineState.Paused) => GeneratorOff.Pick(isChinese),
        (MachineKind.Plants, MachineState.Finished) => PlantsReady.Pick(isChinese),
        (_, MachineState.Running) => Running.Pick(isChinese),
        (_, MachineState.Paused) => Paused.Pick(isChinese),
        (_, MachineState.Idle) => Idle.Pick(isChinese),
        (_, MachineState.Finished) => Done.Pick(isChinese),
        (_, MachineState.OutOfFuel) => OutOfFuel.Pick(isChinese),
        _ => throw new ArgumentOutOfRangeException(nameof(row), row.State, null),
    };

    /// <summary>
    /// Time left, or with the finish clock on, the game time it ends. A paused job, or a generator that is off, is not
    /// counting down, so it always shows time left. A generator's time is its fuel, not a job.
    /// </summary>
    public static string Time(MachineRow row, long nowSeconds, bool isFinishClock, bool isChinese)
    {
        if (row.RemainSeconds is not { } remain || row.State is not (MachineState.Running or MachineState.Paused)) return "";

        var isCountingDown = row.State == MachineState.Running && isFinishClock;
        var shown = isCountingDown ? GameClock.Finish(nowSeconds, remain, isChinese) : GameClock.Left(remain, isChinese);

        if (row.Kind == MachineKind.Generator) return (isCountingDown ? FuelUntil : FuelFor).Say(isChinese, shown);

        return (isCountingDown ? DoneAt : Left).Say(isChinese, shown);
    }

    /// <summary>The timer over a machine with the finish clock on.</summary>
    public static string HeadBarFinish(long nowSeconds, double remainSeconds, bool isChinese) => HeadBarAt.Say(isChinese, GameClock.Finish(nowSeconds, remainSeconds, isChinese));

    public static string Describe(RushOutcome outcome, bool isChinese) => outcome switch
    {
        RushOutcome.Rushed rushed => Rushed.Say(isChinese, rushed.Name, rushed.Cost),
        RushOutcome.TooFew => TooFewPoints.Pick(isChinese),
        RushOutcome.NotRushable => NotRushable.Pick(isChinese),
        RushOutcome.Failed => RushFailed.Pick(isChinese),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };

    public static string ToastTitle(DoneNote note, bool isChinese) => (note.What == DoneWhat.OutOfFuel ? new Phrase("Generator out of fuel", "发电机燃料耗尽") : note.Row.Kind switch
    {
        MachineKind.Brewing => new Phrase("Brew ready", "酒已酿好"),
        MachineKind.Shredder => new Phrase("Shredder finished", "粉碎机已完成"),
        MachineKind.RationPress => new Phrase("Ration Press finished", "口粮压制机已完成"),
        MachineKind.DyeVat => new Phrase("Dye ready", "染料已完成"),
        MachineKind.Cooking => new Phrase("Dish ready", "菜已做好"),
        MachineKind.Drone => new Phrase("Drone back", "无人机已返回"),
        _ => new Phrase("Machine finished", "机器已完成"),
    }).Pick(isChinese);

    /// <summary>The machine's name, and what it made when the game names it.</summary>
    public static string ToastBody(DoneNote note, bool isChinese)
    {
        var name = Name(note.Row, isChinese);

        return note.Row.Contents is Contents.Named { Name: var product } && !string.IsNullOrWhiteSpace(product) ? name + (isChinese ? "：" : ": ") + product : name;
    }
}
