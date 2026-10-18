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
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.machinespeed";
    private const string Version = "0.1.0";
    private const string Section = "Machines";
    private const string ActionSpeedConfig = "com.dev.survivallog.actionspeed.cfg";

    private const string Range = "From 0.25 to 20: 1 is the game's pace, 2 is twice as fast. 0 or less counts as 1.";
    private const string RangeChinese = "范围 0.25 到 20：1 为游戏原速，2 为两倍速。0 或更小按 1 计算。";

    public override void Load()
    {
        var log = new SpeedLog(Log, Path.Combine(Paths.BepInExRootPath, "machine-speed.log"));

        log.TrimIfLarge();

        var isFirstStart = !ReadsAs(Config.ConfigFilePath, text => ConfigText.HasSection(text, Section), log.Write);
        var shredder = Bind("Shredder", "the shredder. It times a job in whole minutes, so a job never takes under 1 minute.", "粉碎机的速度。粉碎按整分钟计时，所以每次至少 1 分钟。");
        var rationPress = Bind("RationPress", "the Ration Press. It times a batch in whole minutes, so a batch never takes under 1 minute.", "口粮压制机的速度。每批按整分钟计时，所以每批至少 1 分钟。");
        var dyeVat = Bind("DyeVat", "the dye vat. It times each bucket in whole hours, so a bucket never takes under 1 hour.", "染料桶的速度。每桶按整小时计时，所以每桶至少 1 小时。");
        var plantGrowth = Bind("PlantGrowth", "plant growth.", "植物生长的速度。");
        var droneTrips = Bind("DroneTrips", "drone trips: a faster trip brings the drone back sooner.", "无人机出行的速度：越快，无人机越早返回。");
        var brewing = Bind("Brewing", "brewing alcohol. It times a brew in whole hours, so a brew never takes under 1 hour.", "酿酒的速度。酿酒按整小时计时，所以每次至少 1 小时。");
        var cooking = Bind("Cooking", "cooking at a stove or hot pot.", "在燃气灶或火锅上烹饪的速度。");
        var fuelBurn = Config.Bind(Section, "FuelBurn", 1f,
            "How fast fuel generators burn fuel: 2 burns it twice as fast, 0.5 makes it last twice as long. From 0.25 to 20; 0 or less counts as 1.\n" +
            "燃油发电机消耗燃料的速度：2 为两倍消耗，0.5 让燃料多用一倍时间。范围 0.25 到 20，0 或更小按 1 计算。");

        if (isFirstStart) ImportFromActionSpeed(brewing, cooking, log.Write);

        var harmony = new Harmony(Guid);

        log.Write($"===== Machine Speed {Version} started");
        ShredderPatch.Apply(harmony, Checked(shredder, log.Write), log);
        RationPressPatch.Apply(harmony, Checked(rationPress, log.Write), log);
        DyeVatPatch.Apply(harmony, Checked(dyeVat, log.Write), log);
        PlantGrowthPatch.Apply(harmony, Checked(plantGrowth, log.Write), log);
        FuelBurnPatch.Apply(harmony, Checked(fuelBurn, log.Write), log);
        DroneTripPatch.Apply(harmony, Checked(droneTrips, log.Write), log);
        BrewingPatch.Apply(harmony, Checked(brewing, log.Write), log);
        CookingPatch.Apply(harmony, Checked(cooking, log.Write), log);
    }

    private ConfigEntry<float> Bind(string key, string work, string workChinese) =>
        Config.Bind(Section, key, 1f, $"Speed of {work} {Range}\n{workChinese}{RangeChinese}");

    private static Speed Checked(ConfigEntry<float> entry, Action<string> log)
    {
        var speed = Speed.From(entry.Value);
        var complaint = speed.Complaint($"{Section}/{entry.Definition.Key}");

        if (complaint != null) log(complaint);

        return speed;
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
