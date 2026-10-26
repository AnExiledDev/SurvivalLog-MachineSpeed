using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// The machine parts of each kind of furniture, asked of the game once per kind. The game gives a piece its parts once,
/// from its table row, when it builds it, so one look at one piece answers for every piece of that kind; asking the
/// game for each part of each piece on every read was most of the cost of a read. A piece not built yet (no parts at
/// all) is answered for but not remembered. Forgotten when the world changes, so nothing outlives a load.
/// </summary>
internal static class FurnitureParts
{
    private static readonly Dictionary<int, MachineParts> ByConfig = new();

    public static int Count => ByConfig.Count;

    public static void Forget() => ByConfig.Clear();

    public static MachineParts Of(Furniture furniture)
    {
        var configId = furniture._AgentConfigId_k__BackingField;

        if (ByConfig.TryGetValue(configId, out var parts)) return parts;

        var components = furniture.agentComponentDictionary;

        if (components == null) return MachineParts.None;

        parts = Probe(furniture, configId);

        if (components.Count > 0) ByConfig[configId] = parts;

        return parts;
    }

    private static MachineParts Probe(Furniture furniture, int configId)
    {
        var parts = MachineParts.None;

        if (AgentTools.GetAgentComponent<DroneTripComponent>(furniture) != null) parts |= MachineParts.Drone;
        if (CompostConfig.IsCompostBin(configId)) parts |= MachineParts.CompostBin;
        if (AgentTools.GetAgentComponent<ShredderComponent>(furniture) != null) parts |= MachineParts.Shredder;
        if (AgentTools.GetAgentComponent<CompressorComponent>(furniture) != null) parts |= MachineParts.RationPress;
        if (AgentTools.GetAgentComponent<DyeComponent>(furniture) != null) parts |= MachineParts.DyeVat;
        if (AgentTools.GetAgentComponent<BrewComponent>(furniture) != null) parts |= MachineParts.Brewing;
        if (AgentTools.GetAgentComponent<CookingComponent>(furniture) != null) parts |= MachineParts.Cooking;
        if (AgentTools.GetAgentComponent<GeneratorFuelComponent>(furniture) != null) parts |= MachineParts.Generator;
        if (AgentTools.GetAgentComponent<PlantComponent>(furniture) != null) parts |= MachineParts.Plant;

        return parts;
    }
}
