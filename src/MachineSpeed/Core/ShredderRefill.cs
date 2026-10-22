namespace MachineSpeed.Core;

/// <summary>
/// An idle shredder starts on its own when items are put into its hopper. Taking items out, sorting the hopper, or a
/// hopper the mod has not seen yet never starts it, so a shredder the player stopped stays stopped until refilled.
/// </summary>
public static class ShredderRefill
{
    /// <param name="before">Items in the hopper when the shredder's panel last showed it; null if it has not shown it yet.</param>
    /// <param name="now">Items in the hopper after the change.</param>
    public static bool ShouldStart(bool isOn, bool isIdle, int? before, int now) =>
        isOn && isIdle && before is { } seen && now > seen;
}
