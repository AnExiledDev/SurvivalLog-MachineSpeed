using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// A growing plant keeps the growth time it was planted with, and grows and counts down on its head bar by that. A
/// plant planted before a change of speed (or in an earlier game) would keep the old speed to the end, so each one is
/// looked at once per speed and put on the current growth time, as far grown as it was.
/// </summary>
internal static class PlantCaches
{
    private static readonly Dictionary<long, float> CheckedAtSpeed = new();
    private static FirstUse _first = FirstUse.Silent;

    public static void UseLog(SpeedLog log) => _first = new FirstUse(log.WriteFileOnly);

    public static void Forget() => CheckedAtSpeed.Clear();

    public static void Resync(BattleLogicWorld world, Speed speed)
    {
        var configs = GameSingleton.Existing<ConfigManager>();
        var furnitures = world._AgentManager?.GetAllFurnitures();

        if (configs == null || furnitures == null) return;

        for (var i = 0; i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture.IsDead) continue;

            var plant = AgentTools.GetAgentComponent<PlantComponent>(furniture);

            if (plant == null || plant.State != PlantState.Growing || IsChecked(furniture.InstanceId, speed)) continue;

            CheckedAtSpeed[furniture.InstanceId] = speed.Value;
            Resync(plant, configs);
        }
    }

    // A plant is marked whether or not it changed, so the game's own small changes (power, the greenhouse) never make
    // it flip back and forth.
    private static bool IsChecked(long id, Speed speed) => CheckedAtSpeed.TryGetValue(id, out var checkedAt) && checkedAt == speed.Value;

    private static void Resync(PlantComponent plant, ConfigManager configs)
    {
        var plantConfig = configs.Get_Config_Plant(plant.PlantConfigId);
        var potConfigs = configs._Config_FurniturePlant_Dict;

        if (plantConfig == null || potConfigs == null || !potConfigs.TryGetValue(plant.FurniturePlantConfigId, out var potConfig) || potConfig == null) return;

        var kept = plant._cachedActualGrowthTime;
        var target = plant.CalcActualGrowthTime(plantConfig, potConfig, -1f);

        if (!PlantResync.IsStale(kept, target)) return;

        var now = plant.GetCurrentTotalSeconds();
        var oldStart = plant.PlantStartTotalSeconds;

        plant.PlantStartTotalSeconds = PlantResync.NewStart(now, oldStart, kept, target);
        plant._cachedActualGrowthTime = target;
        plant.NotifyHeadBarRestore();
        plant.NotifyHudPlantAddOrUpdate();
        _first.Note(() => $"first plant put on the current growth time: {kept:0} -> {target:0}");
    }
}
