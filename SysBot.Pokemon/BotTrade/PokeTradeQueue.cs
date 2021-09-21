using PKHeX.Core;
using System;
using System.Linq;

namespace SysBot.Pokemon
{
    public class PokeTradeQueue<TPoke> where TPoke : PKM, new()
    {
        internal readonly FavoredCPQ<uint, PokeTradeDetail<TPoke>> Queue = new(new FavoredPrioritySettings());

        public readonly PokeTradeType Type;
        public PokeTradeQueue(PokeTradeType type) => Type = type;

        public PokeTradeDetail<TPoke> Find(Func<PokeTradeDetail<TPoke>, bool> match) => Queue.Find(match).Value;

        public int Count => Queue.Count;

        public void Enqueue(PokeTradeDetail<TPoke> detail, uint priority = PokeTradePriorities.TierFree) => Queue.Add(priority, detail);

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
            var list = Queue.Select((x, i) => x.Value.Summary(i + 1));
            return string.Join("\n", list);
        }
    }
}
