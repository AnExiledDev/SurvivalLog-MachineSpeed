using System;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// The "Amounts" settings: how much machines make, hold and use, as against how fast they work. Each setting is bound,
/// watched for live changes and handed to its own patch class, which puts its hooks in only once the setting leaves 1.
/// </summary>
internal static class AmountPatches
{
    private const string Section = "Amounts";

    private const string OutputRange = "Fractions carry over to the next one, so 1.5 gives 1 and 2 by turns. From 1 to 100: less than 1 counts as 1, more than 100 counts as 100.";
    private const string OutputRangeChinese = "小数部分累计到下一次，所以 1.5 会交替给出 1 和 2。范围 1 到 100：小于 1 按 1 计算，大于 100 按 100 计算。";
    private const string RateRange = "From 0.1 to 100: 0 or less counts as 1, between 0 and 0.1 counts as 0.1, more than 100 counts as 100.";
    private const string RateRangeChinese = "范围 0.1 到 100：0 或更小按 1 计算，0 到 0.1 之间按 0.1 计算，大于 100 按 100 计算。";

    public static void Apply(BasePlugin plugin, Harmony harmony, SpeedLog log)
    {
        ShredderOutputPatch.Apply(harmony, log);
        RationPressOutputPatch.Apply(harmony, log);
        BrewingOutputPatch.Apply(harmony, log);
        CompostOutputPatch.Apply(harmony, log);
        HarvestOutputPatch.Apply(harmony, log);
        BarrelCapacityPatch.Apply(harmony, log);
        RobotChargingPatch.Apply(harmony, log);
        FridgeStrengthPatch.Apply(harmony, log);
        EquipmentWearPatch.Apply(harmony, log);

        var config = plugin.Config;

        Watch(config, log, "ShredderOutput", 1f, 100f, ShredderOutputPatch.Use,
            $"How many items the shredder gives for each item it shreds: 1 is the game's amount, 2 is twice as many. {OutputRange}",
            $"粉碎机每粉碎一件物品给出的产出数量：1 为游戏原量，2 为两倍。{OutputRangeChinese}");
        Watch(config, log, "RationPressOutput", 1f, 100f, RationPressOutputPatch.Use,
            $"How many ration blocks and crumbs the Ration Press makes from a batch: 1 is the game's amount, 2 is twice as many. {OutputRange}",
            $"口粮压制机每批做出的口粮块和碎屑数量：1 为游戏原量，2 为两倍。{OutputRangeChinese}");
        Watch(config, log, "BrewingOutput", 1f, 100f, BrewingOutputPatch.Use,
            $"How many bottles a brewing barrel gives for a brew: 1 is the game's amount, 2 is twice as many. {OutputRange}",
            $"酿酒桶每次酿造得到的瓶数：1 为游戏原量，2 为两倍。{OutputRangeChinese}");
        Watch(config, log, "CompostOutput", 1f, 100f, CompostOutputPatch.Use,
            $"How much compost a compost bin makes from the food that rots in it: 1 is the game's amount, 2 is twice as much. Extra compost that does not fit in the bin is lost. {OutputRange}",
            $"堆肥箱用其中腐烂的食物做出的堆肥数量：1 为游戏原量，2 为两倍。箱内放不下的额外堆肥会丢失。{OutputRangeChinese}");
        Watch(config, log, "HarvestOutput", 1f, 100f, HarvestOutputPatch.Use,
            $"How many crops a harvest gives, by hand or by robot: 1 is the game's amount, 2 is twice as many. Seeds are not changed. {OutputRange}",
            $"每次收获（手动或机器人）得到的作物数量：1 为游戏原量，2 为两倍。种子数量不变。{OutputRangeChinese}");
        Watch(config, log, "BarrelCapacity", 1f, 100f, BarrelCapacityPatch.Use,
            "The most bottles a brewing barrel can make in one brew: 1 is the game's limit, 2 doubles it, rounded to whole bottles. The extra bottles still need enough ingredients, and the barrel's ingredient slots do not change. From 1 to 100: less than 1 counts as 1, more than 100 counts as 100.",
            "酿酒桶一次酿造最多能出的瓶数：1 为游戏上限，2 为两倍，按整瓶取整。多出的瓶数仍需足够的原料，酿酒桶的原料格数不变。范围 1 到 100：小于 1 按 1 计算，大于 100 按 100 计算。");
        Watch(config, log, "RobotCharging", 0.1f, 100f, RobotChargingPatch.Use,
            $"How fast robots charge: 1 is the game's rate, 2 is twice as fast. Drones do not charge, so they are not changed. The first change away from 1 reaches robots already built when their modules change or a save is loaded. {RateRange}",
            $"机器人充电的速度：1 为游戏原速，2 为两倍速。无人机不充电，因此不受影响。首次从 1 改为其他值时，已有的机器人会在模块变动或读档后生效。{RateRangeChinese}");
        Watch(config, log, "FridgeStrength", 0.1f, 100f, FridgeStrengthPatch.Use,
            $"How strongly powered fridges and freezers slow spoiling: 2 makes food in them spoil half as fast, 0.5 twice as fast. Food in a fridge never spoils faster than outside one, or slower than in a sealed cold store. In a cold room the game uses whichever is colder, the room or the fridge. {RateRange}",
            $"通电的冰箱和冰柜减缓食物腐烂的强度：2 让其中食物的腐烂速度减半，0.5 让腐烂速度加倍。冰箱里的食物不会比冰箱外腐烂得更快，也不会比密封冷库里更慢。在寒冷房间里，游戏取房间和冰箱中更冷的一方。{RateRangeChinese}");
        Watch(config, log, "EquipmentWear", 0f, 100f, EquipmentWearPatch.Use,
            "How much durability defense devices lose to wear and attacks: 1 is the game's amount, 0.5 is half, 0 means no wear, 2 is twice as much. Doors and windows are not changed, and the wave report still counts the game's wear. Fractions carry over to the next hit. From 0 to 100: less than 0 counts as 1, more than 100 counts as 100.",
            "防御装置因损耗和攻击失去的耐久：1 为游戏原量，0.5 为一半，0 为无损耗，2 为两倍。门窗不受影响，波次战报仍按游戏原本的「装置损耗」计算。小数部分累计到下一次。范围 0 到 100：小于 0 按 1 计算，大于 100 按 100 计算。");
    }

    private static void Watch(ConfigFile config, SpeedLog log, string key, float least, float most, Action<Multiplier> use, string english, string chinese)
    {
        var entry = config.Bind(Section, key, 1f, $"{english}\n{chinese}");

        OptionalLive.Watch(entry, value =>
        {
            var multiplier = Multiplier.From(value, least, most);

            if (multiplier.Complaint($"{Section}/{key}") is { } complaint) log.Write(complaint);

            use(multiplier);
        });
    }
}

/// <summary>
/// One amount setting's hooks, put in the first time the setting leaves 1, so a setting never changed never hooks the
/// game. Once in, the hooks stay and hand back the game's own amounts at 1. A hook that cannot go in leaves the game's
/// amount and is logged as "could not hook", which marks the mod as needing an update after a game update.
/// </summary>
internal sealed class AmountHook
{
    private enum HookState
    {
        Waiting,
        In,
        Failed,
    }

    private readonly SpeedLog _log;
    private readonly string _name;
    private readonly Action _install;
    private readonly Action? _installDisplay;
    private HookState _state = HookState.Waiting;

    public AmountHook(SpeedLog log, string name, Action install, Action? installDisplay = null)
    {
        _log = log;
        _name = name;
        _install = install;
        _installDisplay = installDisplay;
    }

    public bool IsIn => _state == HookState.In;

    /// <summary>Called on the main thread at start and after each change of the setting.</summary>
    public void Use(Multiplier multiplier)
    {
        if (multiplier.IsGameAmount)
        {
            _log.Write($"{_name} left at the game's amount");

            return;
        }

        if (_state == HookState.Waiting)
        {
            Install(multiplier);

            return;
        }

        _log.Write(_state == HookState.In ? $"{_name} x{multiplier.Value:0.##}" : $"{_name} stays at the game's amount, its hook could not go in");
    }

    /// <summary>The display hook only keeps a panel in step, so losing it is logged and the setting still applies.</summary>
    private void Install(Multiplier multiplier)
    {
        try
        {
            _install();
        }
        catch (Exception ex)
        {
            _state = HookState.Failed;
            _log.Write($"could not hook {_name}, it stays at the game's amount: {ex.Message}");

            return;
        }

        _state = HookState.In;

        try
        {
            _installDisplay?.Invoke();
            _log.Write($"{_name} x{multiplier.Value:0.##}");
        }
        catch (Exception ex)
        {
            _log.Write($"{_name} x{multiplier.Value:0.##}, its panel still shows the game's value ({ex.Message})");
        }
    }
}
