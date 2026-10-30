namespace MachineSpeed.Core;

/// <summary>
/// Which rows get a Collect button. Collect queues the game's own take-out, the walk and pick-up a click on the
/// machine queues, and the game has that only for a stove's dish and a brewing barrel's brew. The other machines'
/// products are taken out in their own panels.
/// </summary>
public static class Collecting
{
    public static bool Can(MachineRow row) => row.State == MachineState.Finished && row.Kind is MachineKind.Cooking or MachineKind.Brewing;
}

public abstract record CollectOutcome
{
    private CollectOutcome()
    {
    }

    /// <summary>The walk and the pick-up are in the survivor's queue.</summary>
    public sealed record Queued(string Name) : CollectOutcome;

    /// <summary>The machine is gone, away from home, or has nothing to take out now.</summary>
    public sealed record NotReady : CollectOutcome;

    public sealed record Failed : CollectOutcome;
}
