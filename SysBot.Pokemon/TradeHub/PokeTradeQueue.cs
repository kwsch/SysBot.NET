using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon;

public class PokeTradeQueue<TPoke>(PokeTradeType Type)
    where TPoke : PKM, new()
{
    internal readonly FavoredCPQ<uint, PokeTradeDetail<TPoke>> Queue = new(new FavoredPrioritySettings());
    private readonly Dictionary<int, int> batchProcessingState = [];
    public readonly PokeTradeType Type = Type;

    public PokeTradeDetail<TPoke> Find(Func<PokeTradeDetail<TPoke>, bool> match) => Queue.Find(match).Value;

    public int Count => Queue.Count;

    public void Enqueue(PokeTradeDetail<TPoke> detail, uint priority = PokeTradePriorities.TierFree) => Queue.Add(priority, detail);

    public bool TryDequeue(out PokeTradeDetail<TPoke>? detail, out uint priority)
    {
        while (Queue.TryPeek(out var kvp)) // Peek to check if the trade is processable
        {
            var nextTrade = kvp.Value;
            if (nextTrade.TotalBatchTrades > 1) 
            {
                if (!batchProcessingState.TryGetValue(nextTrade.Code, out _))
                {
                    batchProcessingState[nextTrade.Code] = nextTrade.BatchTradeNumber;
                }

                if (nextTrade.BatchTradeNumber != batchProcessingState[nextTrade.Code])
                {
                    Queue.TryDequeue(out _); 
                    continue; 
                }
            }
            var result = Queue.TryDequeue(out kvp);
            detail = kvp.Value;
            priority = kvp.Key;
            if (detail.TotalBatchTrades > 1)
            {
                batchProcessingState[detail.Code] = batchProcessingState[detail.Code] + 1;
            }
            return result;
        }
        detail = default;
        priority = default;
        return false;
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
