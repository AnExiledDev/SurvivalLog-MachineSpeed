using System.Collections.Generic;

namespace MachineSpeed.Core;

/// <summary>
/// The cooking speed each cook started at. The game charges a cook's fuel as a running total of how far along it is
/// times its length times the fuel rate, so the rate has to stay at the cook's own speed: raising it mid-cook would
/// charge the whole cook so far again at once, and lowering it would make the rest free.
/// </summary>
public sealed class CookSpeeds
{
    private readonly Dictionary<long, Speed> _started = new();
    private readonly Speed _unseen;

    /// <param name="unseen">
    /// The speed for a cook this never saw start: one from the save, or one started before the cooking hook went in.
    /// Both ran at the speed the game started with.
    /// </param>
    public CookSpeeds(Speed unseen) => _unseen = unseen;

    /// <summary>A stove or hot pot started a cook at this speed; its earlier cook is done with.</summary>
    public void Started(long cook, Speed speed) => _started[cook] = speed;

    /// <summary>Another world was loaded, so no cook seen so far is still running.</summary>
    public void Forget() => _started.Clear();

    public Speed For(long cook) => _started.TryGetValue(cook, out var speed) ? speed : _unseen;
}
