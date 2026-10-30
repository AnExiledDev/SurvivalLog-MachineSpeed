using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.Battle.Show;
using MachineSpeed.Core;
using UnityEngine;

namespace MachineSpeed.Adapters;

/// <summary>
/// Sends the survivor to take a finished dish or brew out of its machine. It posts the same request a click on the
/// machine posts (Furniture.ProcessSelect): the game's take-out action, aimed at the spot the survivor stands at to use
/// the machine. The game adds the walk, across floors when it must, and queues both behind whatever the survivor is
/// doing. No hook is involved.
/// </summary>
internal sealed class Collector
{
    private readonly MachineReader _reader;
    private readonly Action<string> _log;

    public Collector(MachineReader reader, Action<string> log)
    {
        _reader = reader;
        _log = log;
    }

    public CollectOutcome Collect(MachineKind kind, long id, bool isChinese)
    {
        if (_reader.Find(kind, id) is not { } found || !Collecting.Can(found.Row)) return new CollectOutcome.NotReady();

        var furniture = found.Furniture;

        try
        {
            var spot = UseSpot(furniture);

            // Target floor 0 is what the click sends: the game finds the machine's floor itself.
            EventDispatcherTools.NotifyGlobal<int, int, long, Vector3, Quaternion>(EnumNotify_MapJump.CrossFloorPlanActionRequest, 0, GameKey.Action_PickupCookingProduct, furniture._InstanceId_k__BackingField, spot, Quaternion.identity);
        }
        catch (Exception ex)
        {
            _log($"collecting from {MachineKinds.Key(kind)} {id} failed: {ex}");

            return new CollectOutcome.Failed();
        }

        _log($"queued a collect from {MachineKinds.Key(kind)} {id}");

        return new CollectOutcome.Queued(OverviewTexts.Name(found.Row, isChinese));
    }

    /// <summary>
    /// The spot the take-out is aimed at, worked out as AgentModelManager.OnSelectFurnitureShow does for a click: the
    /// model's named action point when its table row names one (the model itself for a model in a bag), else the zero
    /// spot the click sends too.
    /// </summary>
    private static Vector3 UseSpot(Furniture furniture)
    {
        var pointName = GameSingleton.Existing<ConfigManager>()?.Get_Config_Furniture(furniture._AgentConfigId_k__BackingField)?.ActionPos;

        if (string.IsNullOrEmpty(pointName)) return Vector3.zero;

        var model = GameSingleton.Existing<BattleShowWorld>()?._AgentModelManager?.GetAgentModel(furniture.ShowInstanceId);

        if (model == null) throw new InvalidOperationException("the machine has no model");
        if (!model.IsBagModel) return model.GetFurniturePos(pointName).Item1;
        if (model.Obj == null) throw new InvalidOperationException("the machine model has no object");

        return model.Obj.transform.position;
    }
}
