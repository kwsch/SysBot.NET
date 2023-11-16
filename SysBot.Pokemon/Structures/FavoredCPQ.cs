using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SysBot.Pokemon;

/// <summary>
/// Allows Enqueue requests to have favored requests inserted ahead of a fraction of unfavored requests.
/// </summary>
public sealed class FavoredCPQ<TKey, TValue> : ConcurrentPriorityQueue<TKey, TValue> where TKey : IComparable<TKey> where TValue : IEquatable<TValue>, IFavoredEntry
{
    public IFavoredCPQSetting Settings { get; set; }

    public FavoredCPQ(IFavoredCPQSetting settings) => Settings = settings;
    public FavoredCPQ(IEnumerable<KeyValuePair<TKey, TValue>> collection, IFavoredCPQSetting settings) : base(collection) => Settings = settings;

    public void Add(TKey priority, TValue value)
    {
        if (Settings.Mode == FavoredMode.None || !value.IsFavored)
        {
            Enqueue(priority, value);
            return;
        }

        lock (_syncLock)
        {
            var q = Queue;
            var items = q.Items;
            int start = items.FindIndex(z => z.Key.Equals(priority));
            if (start < 0) // nobody with this priority in the queue
            {
                // Call directly into the methods since we already reserved the lock
                q.Insert(priority, value);
                return;
            }

            int count = 0;
            int favored = 0;
            int max = items.Count;
            int pos = start;
            while (pos != max)
            {
                var entry = items[pos];
                if (!entry.Key.Equals(priority))
                    break;

                count++;
                if (entry.Value.IsFavored)
                    ++favored;
                ++pos;
            }

            int insertPosition = start + favored + GetInsertPosition(count, favored, Settings);
            if (insertPosition >= items.Count)
                insertPosition = items.Count;

            // Call directly into the methods since we already reserved the lock
            var kvp = new KeyValuePair<TKey, TValue>(priority, value);
            items.Insert(insertPosition, kvp);
        }
    }

    private static int GetInsertPosition(int total, int favored, IFavoredCPQSetting s)
    {
        int unfavored = total - favored;
        int pos = s.Mode switch
        {
            FavoredMode.Exponent => (int)Math.Ceiling(Math.Pow(unfavored, s.Exponent)),
            FavoredMode.Multiply => (int)Math.Ceiling(unfavored * s.Multiply),
            _ => unfavored,
        };

        var clamp = Math.Max(0, pos);
        // If there are enough unfavored users to require our minimum, then clamp.
        if (unfavored >= s.MinimumFreeBypass)
            return Math.Max(s.MinimumFreeAhead, clamp);
        return clamp;
    }
}

public interface IFavoredCPQSetting
{
    FavoredMode Mode { get; }
    float Exponent { get; }
    float Multiply { get; }
    int MinimumFreeAhead { get; }
    int MinimumFreeBypass { get; }
}

public enum FavoredMode
{
    None,
    Exponent,
    Multiply,
}

public interface IFavoredEntry
{
    bool IsFavored { get; }
}
