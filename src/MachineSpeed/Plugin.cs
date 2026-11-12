using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using HarmonyLib;
using MachineSpeed.Adapters;
using MachineSpeed.Core;
using static MachineSpeed.Adapters.WorldRule;

namespace MachineSpeed;

[BepInPlugin(Guid, "Machine Speed", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
[BepInDependency(DayLengthWorld.DayLengthGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.machinespeed";
    private const string Version = "0.9.3";
    private const string Section = "Machines";
    private const string ActionSpeedConfig = "com.dev.survivallog.actionspeed.cfg";

    /// <summary>Exiled's Toolkit: its shared helpers, toasts, live settings and UI size all come from it.</summary>
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.19.0";

    private const string LogFileName = "machine-speed.log";

    /// <summary>Past this the log file is moved to machine-speed.log.1 and started again.</summary>
    private const long MaxLogBytes = 1024 * 1024;

    /// <summary>The name the page script hands its window to the Toolkit's UI size with.</summary>
    private const string UiSizeName = "MachineSpeed";

    private const string Range = "From 0.25 to 100000: 1 is the game's pace, 2 is twice as fast, a very high number makes it almost instant. 0 or less counts as 1.";
    private const string RangeChinese = "范围 0.25 到 100000：1 为游戏原速，2 为两倍速，数值很大时几乎立即完成。0 或更小按 1 计算。";

    public override void Load()
    {
        var log = new ModLog(Log, LogFileName) { MaxFileBytes = MaxLogBytes };
        KeepRenamedPreset(log.Write);
        var isFirstStart = !ReadsAs(Config.ConfigFilePath, text => ConfigText.HasSection(text, Section), log.Write);
        var shredder = Bind("Shredder", "the shredder.", "粉碎机的速度。");
        var rationPress = Bind("RationPress", "the Ration Press.", "口粮压制机的速度。");
        var dyeVat = Bind("DyeVat", "the dye vat.", "染料桶的速度。");
        var plantGrowth = Bind("PlantGrowth", "plant growth.", "植物生长的速度。");
        var droneTrips = Bind("DroneTrips", "drone trips: a faster trip brings the drone back sooner.", "无人机出行的速度：越快，无人机越早返回。");
        var brewing = Bind("Brewing", "brewing alcohol.", "酿酒的速度。");
        var cooking = Bind("Cooking", "cooking at a stove or hot pot.", "在燃气灶或火锅上烹饪的速度。");
        var compost = Bind("Compost", "compost bins turning food into compost.", "堆肥箱把食物变成堆肥的速度。");
        ModUiSize.Bind(Config, UiSizeName);

        var cookingFuel = Config.Bind(Section, "CookingFuel", 1f, Rule(
            "How much fuel a stove or hot pot uses for each dish, whatever the cooking speed: 1 is the game's amount, 0.5 is half, 0 uses none, 2 is twice as much. From 0 to 20, and less than 0 counts as 1.\n" +
            "燃气灶或火锅每道菜消耗的燃料，与烹饪速度无关：1 为游戏原量，0.5 为一半，0 不消耗，2 为两倍。范围 0 到 20，小于 0 按 1 计算。"));
        var preset = Config.Bind(Section, "Preset", Preset.Custom, Rule(
            "Sets every machine speed at once. Custom uses the speed you set for each machine. The others set every machine to normal speed (Default), 2x (Relaxed), 5x (Fast) or 100000x (Instant), which is almost instant. The preset does not change CookingFuel.\n" +
            "一次设置所有机器的速度。Custom 使用你为每台机器设的速度。其他预设把所有机器设为正常速度（Default）、两倍速（Relaxed）、五倍速（Fast）或 100000 倍速（Instant，几乎立即完成）。预设不会改动 CookingFuel。"));
        var followDayLength = Config.Bind(Section, "FollowDayLength", false, Rule(
            "For the Day Length mod. When it makes days longer or shorter, machines keep the same pace in real time. With days twice as long, every machine speed is doubled, on top of the preset. It picks up Day Length's setting when a save loads. Does nothing without Day Length. CookingFuel is not changed.\n" +
            "配合「昼夜时长」模组使用。它让一天变长或变短时，机器在现实时间里的工作节奏保持不变。一天变为两倍长时，所有机器速度也翻倍，并叠加在预设之上。读取存档时跟随「昼夜时长」的设置。未安装「昼夜时长」时不起作用。CookingFuel 不受影响。"));
        var shredderAutoStart = Config.Bind(Section, "ShredderAutoStart", false,
            "Start an idle shredder by itself when you put items into it, as if you pressed Start. Taking items out does not start it, so a shredder you stopped stays stopped until you add more. It still needs power and room for what it makes.\n" +
            "向闲置的粉碎机放入物品时自动开始粉碎，如同按下开始按钮。取出物品不会让它开始，所以你停下的粉碎机在再次放入物品前保持停止。粉碎机仍需要电力和放产物的空间。");

        if (isFirstStart) ImportFromActionSpeed(brewing, cooking, log.Write);

        var hooks = new PatchKit(new Harmony(Guid), log.Write);

        log.Write($"===== Machine Speed {Version} started");
        ShredderPatch.Apply(hooks, log);
        RationPressPatch.Apply(hooks, log);
        PlantGrowthPatch.Apply(hooks, log);
        DroneTripPatch.Apply(hooks, log);
        CookingPatch.Apply(hooks, log);
        CookingFuelPatch.Apply(hooks, log);
        BrewingPatch.Apply(hooks, log);
        DyeVatPatch.Apply(hooks, log);
        CompostPatch.Apply(log);
        ShredderAutoStart.Apply(hooks, log);
        AmountPatches.Apply(this, hooks, log);
        HourJobs.ReportFailuresTo(hooks);
        TimerPage.Start(log);

        var speeds = new PlannedSpeeds(preset, log.Write);

        DayLengthWorld.Apply(hooks, log, speeds.Reapply);
        speeds.Watch(shredder, ShredderPatch.Use);
        speeds.Watch(rationPress, RationPressPatch.Use);
        speeds.Watch(plantGrowth, PlantGrowthPatch.Use);
        speeds.Watch(droneTrips, DroneTripPatch.Use);
        speeds.Watch(brewing, BrewingPatch.Use);
        speeds.Watch(dyeVat, DyeVatPatch.Use);
        speeds.Watch(compost, CompostPatch.Use);
        speeds.Watch(() =>
        {
            var cookingSpeed = speeds.For(cooking);
            var fuel = FuelUse.From(cookingFuel.Value);

            if (fuel.Complaint($"{Section}/{cookingFuel.Definition.Key}") is { } fuelComplaint) log.Write(fuelComplaint);

            CookingPatch.Use(cookingSpeed);
            CookingFuelPatch.Use(cookingSpeed, fuel);
        }, cooking, cookingFuel);
        LiveSettings.Watch(followDayLength, DayLengthWorld.Use);
        LiveSettings.Watch(shredderAutoStart, ShredderAutoStart.Use);
        Overview.Install(this, Config, hooks, log);
        StartClock(log);
    }

    /// <summary>
    /// Brewing, dyeing and compost are moved along a few times a second by a ticker. It runs even with all three at the
    /// game's pace, so speeding one up later needs no restart; with nothing tracked, a tick does nothing.
    /// </summary>
    private void StartClock(ModLog log)
    {
        try
        {
            AddComponent<MachineTicker>();
        }
        catch (Exception ex)
        {
            log.Write($"could not start the clock for brewing, dyeing and compost, they stay at the game's pace: {ex.Message}");
        }
    }

    private ConfigEntry<float> Bind(string key, string work, string workChinese) =>
        Config.Bind(Section, key, 1f, Rule($"Speed of {work} {Range}\n{workChinese}{RangeChinese}"));

    /// <summary>
    /// The Default preset had another name before 0.9.3. BepInEx can't read the old name any more and would fall back to
    /// Custom with a warning, so the saved line is rewritten before anything is bound.
    /// </summary>
    private void KeepRenamedPreset(Action<string> log)
    {
        var renamed = ReadsAs<string?>(Config.ConfigFilePath, ConfigText.RenamedPreset, log);

        if (renamed is null) return;

        try
        {
            File.WriteAllText(Config.ConfigFilePath, renamed);
            Config.Reload();
        }
        catch (Exception ex)
        {
            log($"could not rename the saved {ConfigText.OldDefaultPresetName} preset to Default: {ex.Message}");

            return;
        }

        log($"the {ConfigText.OldDefaultPresetName} preset is now called Default, kept your choice");
    }

    /// <summary>Brewing and cooking used to be Action Speed settings; the first start carries a player's values over.</summary>
    private void ImportFromActionSpeed(ConfigEntry<float> brewing, ConfigEntry<float> cooking, Action<string> log)
    {
        var path = Path.Combine(Paths.ConfigPath, ActionSpeedConfig);

        if (!File.Exists(path)) return;

        var old = ReadsAs(path, ConfigText.ReadOldActionSpeed, log);

        if (old.Brewing is { } brew) brewing.Value = brew;

        if (old.Cooking is { } cook) cooking.Value = cook;

        if (old.Brewing == null && old.Cooking == null) return;

        try
        {
            Config.Save();
        }
        catch (Exception ex)
        {
            log($"could not save the brewing and cooking speeds taken from Action Speed: {ex.Message}");
        }

        log($"took brewing x{brewing.Value:0.##} and cooking x{cooking.Value:0.##} from Action Speed's config");
    }

    /// <summary>A file that is missing or cannot be read reads as empty, so the plugin still loads.</summary>
    private static T ReadsAs<T>(string path, Func<string, T> parse, Action<string> log)
    {
        try
        {
            return parse(File.Exists(path) ? File.ReadAllText(path) : "");
        }
        catch (Exception ex)
        {
            log($"could not read {Path.GetFileName(path)}: {ex.Message}");

            return parse("");
        }
    }
}
