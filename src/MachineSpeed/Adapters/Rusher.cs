using System;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Finishes one running job now for survival points. Each machine is finished by moving its start (or a drone's
/// return) back so that the game's own update, or its own finish call, completes the job the usual way: the product
/// lands where the game puts it and every follow-up the game runs still runs. Points are spent only after the job was
/// finished, so a finish that throws costs nothing.
/// </summary>
internal sealed class Rusher
{
    private const int SecondsPerHour = 3600;

    private readonly MachineReader _reader;
    private readonly Action<string> _log;

    public Rusher(MachineReader reader, Action<string> log)
    {
        _reader = reader;
        _log = log;
    }

    public RushOutcome Rush(MachineKind kind, long id, double pointsPerHour, bool isChinese)
    {
        if (_reader.Find(kind, id) is not { } found) return new RushOutcome.NotRushable();

        var points = _reader.Points(found.World);

        if (points is not { } have) return new RushOutcome.NotRushable();

        switch (RushCost.Plan(found.Row, have, pointsPerHour))
        {
            case RushPlan.TooFew tooFew:
                return new RushOutcome.TooFew(tooFew.Cost, tooFew.Points);
            case RushPlan.Affordable affordable:
                return Finish(found, affordable.Cost, isChinese);
            default:
                return new RushOutcome.NotRushable();
        }
    }

    private RushOutcome Finish(FoundMachine found, int cost, bool isChinese)
    {
        var name = OverviewTexts.Name(found.Row, isChinese);

        try
        {
            if (!Complete(found)) return new RushOutcome.NotRushable();
        }
        catch (Exception ex)
        {
            _log($"rushing {MachineKinds.Key(found.Row.Kind)} {found.Row.Id} failed, no survival points were spent: {ex}");

            return new RushOutcome.Failed();
        }

        Spend(found, cost);

        return new RushOutcome.Rushed(name, cost);
    }

    /// <summary>The job is already finished here, so a refused or failed spend is only logged.</summary>
    private void Spend(FoundMachine found, int cost)
    {
        var machine = $"{MachineKinds.Key(found.Row.Kind)} {found.Row.Id}";

        try
        {
            if (found.World._SurvivalResultsManager.TrySpendSurvivalPoint(cost)) _log($"rushed {machine} for {cost} survival points");
            else _log($"rushed {machine}, but the game refused to take {cost} survival points");
        }
        catch (Exception ex)
        {
            _log($"rushed {machine}, but taking {cost} survival points failed: {ex}");
        }
    }

    /// <summary>False when the machine has no part this kind of rush needs; the reader only offers Rush where it does.</summary>
    private static bool Complete(FoundMachine found)
    {
        var furniture = found.Furniture;
        var now = found.NowSeconds;
        var nowHour = now / SecondsPerHour;

        switch (found.Row.Kind)
        {
            case MachineKind.Shredder when AgentTools.GetAgentComponent<ShredderComponent>(furniture) is { } shredder:
                shredder.StartTotalSeconds = now - shredder.TotalSeconds;
                return true;
            case MachineKind.RationPress when AgentTools.GetAgentComponent<CompressorComponent>(furniture) is { } press:
                press.StartTotalSeconds = now - press.TotalSeconds;
                return true;
            case MachineKind.Brewing when AgentTools.GetAgentComponent<BrewComponent>(furniture) is { } brew:
                brew.StartHour = nowHour - brew.TotalHours;
                furniture.TryFinishBrew();
                return true;
            case MachineKind.DyeVat when AgentTools.GetAgentComponent<DyeComponent>(furniture) is { } dye:
                dye.StartHour = nowHour - dye.ResultCount * dye.HoursPerBucket;
                furniture.TryFinishDye();
                return true;
            case MachineKind.Cooking when AgentTools.GetAgentComponent<CookingComponent>(furniture) is { } cooking:
                cooking.CookStartTotalSeconds = now - cooking.CookDuration;
                return true;
            case MachineKind.Drone when AgentTools.GetAgentComponent<DroneTripComponent>(furniture) is { } drone:
                drone.ReturnSec = now;
                return true;
            default:
                return false;
        }
    }
}
