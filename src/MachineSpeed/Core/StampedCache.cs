using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MachineSpeed.Core;

/// <summary>
/// Values worked out once and kept while what they came from stays the same: the stamp. A new stamp (another loaded
/// game, the game's tables reloaded, another language, the next frame) drops every value kept under the old one, so a
/// value from before can never be handed out after.
/// </summary>
public sealed class StampedCache<TStamp, TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _values = new();
    private TStamp _stamp = default!;
    private bool _hasStamp;

    public int Count => _values.Count;

    /// <summary>Called before reading under a stamp; true when the stamp changed and everything kept was dropped.</summary>
    public bool Use(TStamp stamp)
    {
        if (_hasStamp && EqualityComparer<TStamp>.Default.Equals(_stamp, stamp)) return false;

        _values.Clear();
        _stamp = stamp;
        _hasStamp = true;

        return true;
    }

    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value) => _values.TryGetValue(key, out value);

    public void Set(TKey key, TValue value) => _values[key] = value;

    /// <summary>Drops everything, and the stamp with it, so the next <see cref="Use"/> starts afresh whatever it is given.</summary>
    public void Forget()
    {
        _values.Clear();
        _stamp = default!;
        _hasStamp = false;
    }
}
