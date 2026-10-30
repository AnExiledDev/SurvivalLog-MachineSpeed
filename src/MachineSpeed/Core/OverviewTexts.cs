using System;
using System.Globalization;

namespace MachineSpeed.Core;

/// <summary>One piece of on-screen text in the two languages the game ships. {0}, {1} are filled in by Say.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;

    public string Say(bool isChinese, params object[] args) => string.Format(CultureInfo.InvariantCulture, Pick(isChinese), args);
}

/// <summary>A time as the window writes it: the number, then the small word after it ("left", "done").</summary>
public readonly record struct TimeText(string Value, string Word);

/// <summary>A row's times: the big one, and the small one under it when there is one.</summary>
public readonly record struct RowClock(TimeText Main, TimeText? Other);

/// <summary>The words of the Machines window, its HUD button, the Rush button and the done toasts.</summary>
public static class OverviewTexts
{
    public static readonly Phrase Title = new("Machines", "机器总览");
    public static readonly Phrase ButtonTip = new("Every machine at home: what it is making and when it is done.", "基地里的所有机器：正在制作什么、何时完成。");
    public static readonly Phrase Away = new("Machines are listed at home only.", "只在基地时列出机器。");
    public static readonly Phrase NoGame = new("Load a game first.", "请先载入游戏。");
    public static readonly Phrase Empty = new("No machines at home yet.", "基地里还没有机器。");
    public static readonly Phrase PointsLabel = new("Survival Points", "生存点");
    public static readonly Phrase AtHome = new("{0} at home", "基地里 {0} 台");
    public static readonly Phrase NeedYou = new("{0} need you", "{0} 台待处理");
    public static readonly Phrase NeedsSection = new("Needs you", "待处理");
    public static readonly Phrase WorkingSection = new("Working", "进行中");
    public static readonly Phrase IdleSection = new("Idle", "闲置");
    public static readonly Phrase ToClose = new("to close", "关闭");

    public static readonly Phrase Running = new("Running", "运行中");
    public static readonly Phrase Paused = new("Paused", "已暂停");
    public static readonly Phrase Idle = new("Idle", "闲置");
    public static readonly Phrase Done = new("Done", "完成");
    public static readonly Phrase OutOfFuel = new("Out of fuel", "燃料耗尽");
    public static readonly Phrase DroneBack = new("Back", "成功返回");
    public static readonly Phrase GeneratorOff = new("Off", "已关闭");
    public static readonly Phrase PlantsReady = new("Ready", "可收获");

    public static readonly Phrase LeftWord = new("left", "剩余");
    public static readonly Phrase DoneWord = new("done", "完成");
    public static readonly Phrase FuelLeftWord = new("fuel left", "燃料剩余");
    public static readonly Phrase FuelUntilWord = new("fuel until", "燃料可用到");

    public static readonly Phrase HintTakeOut = new("Ready to take out", "可以取出");
    public static readonly Phrase HintDroneBack = new("Back with its haul", "已带回物资");
    public static readonly Phrase HintHarvest = new("Ready to harvest", "可以收获");
    public static readonly Phrase HintAddFuel = new("Add fuel to restart", "加燃料后继续");
    public static readonly Phrase HintNothingLoaded = new("Nothing loaded", "没有放入材料");
    public static readonly Phrase HintDroneHome = new("Ready to send out", "可以派出");
    public static readonly Phrase HintPaused = new("Paused at the machine", "在机器上暂停");
    public static readonly Phrase HintSwitchedOff = new("Switched off", "已关闭");
    public static readonly Phrase HeadBarAt = new("at {0}", "{0}完成");

    public static readonly Phrase OneItem = new("1 item", "1 件物品");
    public static readonly Phrase ManyItems = new("{0} items", "{0} 件物品");
    public static readonly Phrase PlantCounts = new("{0} growing, {1} ready", "{0} 株生长中，{1} 株可收获");

    public static readonly Phrase RushLabel = new("Rush", "加速完成");
    public static readonly Phrase RushTip = new("Finish now for {0} survival points", "花费 {0} 生存点立即完成");
    public static readonly Phrase RushTooFewTip = new("Needs {0} survival points, you have {1}", "需要生存点：{0}，拥有生存点：{1}");
    public static readonly Phrase RushTooFewShort = new("Need {0}, have {1}", "需要 {0}，拥有 {1}");
    public static readonly Phrase Rushed = new("Finished {0} for {1} survival points", "已花费 {1} 生存点完成「{0}」");
    public static readonly Phrase TooFewPoints = new("Not enough survival points", "生存点不足");
    public static readonly Phrase NotRushable = new("This machine cannot be rushed now", "此机器现在无法加速完成");
    public static readonly Phrase RushFailed = new("Rush failed, nothing was spent", "加速完成失败，未花费生存点");

    public static readonly Phrase CollectLabel = new("Collect", "收取");
    public static readonly Phrase CollectQueued = new("Going to collect from {0}", "前往「{0}」收取");
    public static readonly Phrase NothingToCollect = new("Nothing to collect here now", "这台机器现在没有可收取的东西");
    public static readonly Phrase CollectFailed = new("Could not send your survivor to collect", "无法前往收取");

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
    /// The row's two times, the big one first: time left, then the game time it ends; the finish clock swaps them. A
    /// paused job, or a generator that is off, is not counting down, so it shows time left alone. A generator's time is
    /// its fuel, not a job. Null when the row has no time to show.
    /// </summary>
    public static RowClock? Clock(MachineRow row, long nowSeconds, bool isFinishClock, bool isChinese)
    {
        if (row.RemainSeconds is not { } remain || row.State is not (MachineState.Running or MachineState.Paused)) return null;

        var isGenerator = row.Kind == MachineKind.Generator;
        var left = new TimeText(GameClock.Left(remain, isChinese), (isGenerator ? FuelLeftWord : LeftWord).Pick(isChinese));

        if (row.State != MachineState.Running) return new RowClock(left, null);

        var finish = new TimeText(GameClock.Finish(nowSeconds, remain, isChinese), (isGenerator ? FuelUntilWord : DoneWord).Pick(isChinese));

        return isFinishClock ? new RowClock(finish, left) : new RowClock(left, finish);
    }

    /// <summary>What the player should do about a row, or why it is stopped; empty for a job that is simply running.</summary>
    public static string Hint(MachineRow row, bool isChinese) => (row.Kind, row.State) switch
    {
        (MachineKind.Drone, MachineState.Finished) => HintDroneBack.Pick(isChinese),
        (MachineKind.Plants, MachineState.Finished) => HintHarvest.Pick(isChinese),
        (MachineKind.Drone, MachineState.Idle) => HintDroneHome.Pick(isChinese),
        (MachineKind.Generator, MachineState.Paused) => HintSwitchedOff.Pick(isChinese),
        (_, MachineState.Finished) => HintTakeOut.Pick(isChinese),
        (_, MachineState.OutOfFuel) => HintAddFuel.Pick(isChinese),
        (_, MachineState.Idle) => HintNothingLoaded.Pick(isChinese),
        (_, MachineState.Paused) => HintPaused.Pick(isChinese),
        (_, MachineState.Running) => "",
        _ => throw new ArgumentOutOfRangeException(nameof(row), row.State, null),
    };

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

    public static string Describe(CollectOutcome outcome, bool isChinese) => outcome switch
    {
        CollectOutcome.Queued queued => CollectQueued.Say(isChinese, queued.Name),
        CollectOutcome.NotReady => NothingToCollect.Pick(isChinese),
        CollectOutcome.Failed => CollectFailed.Pick(isChinese),
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
