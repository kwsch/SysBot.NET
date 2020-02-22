using System;
using System.Collections.Concurrent;
using System.Linq;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1158:Static member in generic type should use a type parameter.", Justification = "<Pending>")]
    public class PokeTradeQueue<TPoke> where TPoke : PKM, new()
    {
        public const uint Tier1 = 1;
        public const uint Tier2 = 2;
        public const uint Tier3 = 3;
        public const uint Tier4 = 4;
        public const uint TierFree = uint.MaxValue;
        private readonly ConcurrentPriorityQueue<uint, PokeTradeDetail<TPoke>> Queue = new ConcurrentPriorityQueue<uint, PokeTradeDetail<TPoke>>();

        public PokeTradeDetail<TPoke> Find(Func<PokeTradeDetail<TPoke>, bool> match) => Queue.Find(match).Value;

        public int Count => Queue.Count;

        public void Enqueue(PokeTradeDetail<TPoke> detail, uint priority = TierFree) => Queue.Enqueue(priority, detail);

        public bool TryDequeue(out PokeTradeDetail<TPoke> detail, out uint priority)
        {
            var result = Queue.TryDequeue(out var kvp);
            detail = kvp.Value;
            priority = kvp.Key;
            return result;
        }

        public bool TryPeek(out PokeTradeDetail<TPoke> detail, out uint priority)
        {
            var result = Queue.TryPeek(out var kvp);
            detail = kvp.Value;
            priority = kvp.Key;
            return result;
        }

        public void Clear() => Queue.Clear();
        public int Remove(PokeTradeDetail<TPoke> detail) => Queue.Remove(detail);
        public int IndexOf(PokeTradeDetail<TPoke> detail) => Queue.IndexOf(detail);

        public string Summary()
        {
            var list = Queue.Select((x, i) => x.Value.Summary(i));
            return string.Join("\n", list);
        }
    }
}