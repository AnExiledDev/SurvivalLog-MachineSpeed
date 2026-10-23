using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MachineSpeed.Adapters;
using MachineSpeed.Core;

namespace MachineSpeed;

[BepInPlugin(Guid, "Machine Speed", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(DayLengthWorld.DayLengthGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.machinespeed";
    private const string Version = "0.5.2";
    private const string Section = "Machines";
    private const string ActionSpeedConfig = "com.dev.survivallog.actionspeed.cfg";

    private const string Range = "From 0.25 to 100000: 1 is the game's pace, 2 is twice as fast, a very high number makes it almost instant. 0 or less counts as 1.";
    private const string RangeChinese = "范围 0.25 到 100000：1 为游戏原速，2 为两倍速，数值很大时几乎立即完成。0 或更小按 1 计算。";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var log = new SpeedLog(Log, Path.Combine(Paths.BepInExRootPath, "machine-speed.log"));

        log.TrimIfLarge();

        var isFirstStart = !ReadsAs(Config.ConfigFilePath, text => ConfigText.HasSection(text, Section), log.Write);
        var shredder = Bind("Shredder", "the shredder.", "粉碎机的速度。");
        var rationPress = Bind("RationPress", "the Ration Press.", "口粮压制机的速度。");
        var dyeVat = Bind("DyeVat", "the dye vat.", "染料桶的速度。");
        var plantGrowth = Bind("PlantGrowth", "plant growth.", "植物生长的速度。");
        var droneTrips = Bind("DroneTrips", "drone trips: a faster trip brings the drone back sooner.", "无人机出行的速度：越快，无人机越早返回。");
        var brewing = Bind("Brewing", "brewing alcohol.", "酿酒的速度。");
        var cooking = Bind("Cooking", "cooking at a stove or hot pot.", "在燃气灶或火锅上烹饪的速度。");
        var compost = Bind("Compost", "compost bins turning food into compost.", "堆肥箱把食物变成堆肥的速度。");
        var cookingFuel = Config.Bind(Section, "CookingFuel", 1f,
            "How much fuel a stove or hot pot uses for each dish, whatever the cooking speed: 1 is the game's amount, 0.5 is half, 0 uses none, 2 is twice as much. From 0 to 20; less than 0 counts as 1.\n" +
            "燃气灶或火锅每道菜消耗的燃料，与烹饪速度无关：1 为游戏原量，0.5 为一半，0 不消耗，2 为两倍。范围 0 到 20，小于 0 按 1 计算。");
        var fuelBurn = Config.Bind(Section, "FuelBurn", 1f,
            "How fast fuel generators burn fuel: 2 burns it twice as fast, 0.5 makes it last twice as long. From 0.25 to 100000; 0 or less counts as 1.\n" +
            "燃油发电机消耗燃料的速度：2 为两倍消耗，0.5 让燃料多用一倍时间。范围 0.25 到 100000，0 或更小按 1 计算。");
        var preset = Config.Bind(Section, "Preset", Preset.Custom,
            "Sets every machine speed at once. Custom uses each machine's own speed setting; Vanilla is the game's pace; Relaxed is 2x; Fast is 5x; Instant is 100000x, almost instant. FuelBurn and CookingFuel keep their own settings.\n" +
            "一次设置所有机器的速度。Custom 使用各台机器自己的速度设置；Vanilla 为游戏原速；Relaxed 为两倍速；Fast 为五倍速；Instant 为 100000 倍速，几乎立即完成。FuelBurn 和 CookingFuel 仍按各自的设置。");
        var followDayLength = Config.Bind(Section, "FollowDayLength", false,
            "When the Day Length mod makes days longer or shorter, machines keep their pace in real time: with days twice as long, every machine speed is doubled, on top of the preset. Day Length applies its setting when a save loads, and this follows it from then. Does nothing without Day Length. FuelBurn and CookingFuel are not changed.\n" +
            "当「昼夜时长」模组让一天变长或变短时，机器在现实时间里保持原来的工作节奏：一天变为两倍长时，所有机器速度也翻倍，并叠加在预设之上。「昼夜时长」在读取存档时应用其设置，本项从那时起跟随。未安装「昼夜时长」时不起作用。FuelBurn 和 CookingFuel 不受影响。");
        var shredderAutoStart = Config.Bind(Section, "ShredderAutoStart", false,
            "Start an idle shredder by itself when you put items into it, as if you pressed Start. Taking items out does not start it, so a shredder you stopped stays stopped until you add more. It still needs power and room for what it makes.\n" +
            "向闲置的粉碎机放入物品时自动开始粉碎，如同按下开始按钮。取出物品不会让它开始，所以你停下的粉碎机在再次放入物品前保持停止。粉碎机仍需要电力和放产物的空间。");

        if (isFirstStart) ImportFromActionSpeed(brewing, cooking, log.Write);

        var harmony = new Harmony(Guid);

        log.Write($"===== Machine Speed {Version} started");
        ShredderPatch.Apply(harmony, log);
        RationPressPatch.Apply(harmony, log);
        PlantGrowthPatch.Apply(harmony, log);
        FuelBurnPatch.Apply(harmony, log);
        DroneTripPatch.Apply(harmony, log);
        CookingPatch.Apply(harmony, log);
        CookingFuelPatch.Apply(harmony, log);
        BrewingPatch.Apply(harmony, log);
        DyeVatPatch.Apply(harmony, log);
        CompostPatch.Apply(log);
        ShredderAutoStart.Apply(harmony, log);
        AmountPatches.Apply(this, harmony, log);
        HourJobs.UseLog(log);
        TimerPage.UseLog(log);

        var speeds = new PlannedSpeeds(preset, log.Write);

        DayLengthWorld.Apply(harmony, log, speeds.Reapply);
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
        OptionalLive.Watch(fuelBurn, _ => FuelBurnPatch.Use(speeds.Own(fuelBurn)));
        OptionalLive.Watch(followDayLength, DayLengthWorld.Use);
        OptionalLive.Watch(shredderAutoStart, ShredderAutoStart.Use);
        Overview.Install(this, Config, harmony, log);
        StartClock(log);
    }

    /// <summary>
    /// Brewing, dyeing and compost are moved along a few times a second by a ticker. It runs even with all three at the
    /// game's pace, so speeding one up later needs no restart; with nothing tracked, a tick does nothing.
    /// </summary>
    private void StartClock(SpeedLog log)
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
        Config.Bind(Section, key, 1f, $"Speed of {work} {Range}\n{workChinese}{RangeChinese}");

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
