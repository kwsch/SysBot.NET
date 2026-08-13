using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon;

/// <summary>
/// Contains a queue of users to be processed.
/// </summary>
/// <typeparam name="T">Type of data to be transmitted to the users</typeparam>
public sealed record TradeQueueInfo<T>(PokeTradeHub<T> Hub)
    where T : PKM, new()
{
    private readonly Lock _sync = new();

    /// <summary>
    /// Currently queued users, including those currently being handled via trade bots (actively trading).
    /// </summary>
    /// <remarks>
    /// We need to keep track of users currently being traded. They can re-join AFTER their trade completes.
    /// </remarks>
    private readonly List<TradeEntry<T>> _queue = [];
    public readonly PokeTradeHub<T> Hub = Hub;

    public int Count
    {
        get
        {
            lock (_sync)
                return _queue.Count;
        }
    }

    public bool ToggleQueue() => Hub.Config.Queues.CanQueue ^= true;

    public bool GetCanQueue()
    {
        if (!Hub.Config.Queues.CanQueue)
            return false;
        lock (_sync)
            return _queue.Count < Hub.Config.Queues.MaxQueueCount && Hub.TradeBotsReady;
    }

    public TradeEntry<T>? GetDetail(ulong uid)
    {
        lock (_sync)
            return _queue.Find(z => z.UserID == uid);
    }

    public QueueCheckResult<T> CheckPosition(ulong uid, PokeRoutineType type = 0)
    {
        lock (_sync)
        {
            var index = _queue.FindIndex(z => z.Equals(uid, type));
            if (index < 0)
                return QueueCheckResult<T>.None;

            var entry = _queue[index];
            var actualIndex = 1;
            for (int i = 0; i < index; i++)
            {
                if (_queue[i].Type == entry.Type)
                    actualIndex++;
            }

            var inQueue = _queue.Count(z => z.Type == entry.Type);

            return new QueueCheckResult<T>(true, entry, actualIndex, inQueue);
        }
    }

    public string GetPositionString(ulong uid, PokeRoutineType type = PokeRoutineType.Idle)
    {
        var check = CheckPosition(uid, type);
        return check.GetMessage();
    }

    public string GetTradeList(PokeRoutineType t)
    {
        lock (_sync)
        {
            var queue = Hub.Queues.GetQueue(t);
            if (queue.Count == 0)
                return "Nobody in queue.";
            return queue.Summary();
        }
    }

    public void ClearAllQueues()
    {
        lock (_sync)
        {
            Hub.Queues.ClearAll();
            _queue.Clear();
        }
    }

    public QueueResultRemove ClearTrade(string userName)
    {
        var details = GetIsUserQueued(z => z.Username == userName);
        return ClearTrade(details);
    }

    public QueueResultRemove ClearTrade(ulong userId)
    {
        var details = GetIsUserQueued(z => z.UserID == userId);
        return ClearTrade(details);
    }

    private QueueResultRemove ClearTrade(IReadOnlyCollection<TradeEntry<T>> details)
    {
        if (details.Count == 0)
            return QueueResultRemove.NotInQueue;

        int removedCount = ClearTrade(details, Hub);

        if (removedCount == details.Count)
            return QueueResultRemove.Removed;

        bool canRemoveWhileProcessing = Hub.Config.Queues.CanDequeueIfProcessing;
        foreach (var detail in details)
        {
            if (detail.Trade.IsProcessing && !canRemoveWhileProcessing)
                continue;
            Remove(detail);
        }

        return canRemoveWhileProcessing
            ? QueueResultRemove.CurrentlyProcessingRemoved
            : QueueResultRemove.CurrentlyProcessing;
    }

    public int ClearTrade(IEnumerable<TradeEntry<T>> details, PokeTradeHub<T> hub)
    {
        int removedCount = 0;
        lock (_sync)
        {
            var queues = hub.Queues.AllQueues;
            foreach (var detail in details)
            {
                if (detail.Trade.IsProcessing && !Hub.Config.Queues.CanDequeueIfProcessing)
                    continue;
                foreach (var queue in queues)
                {
                    int removed = queue.Remove(detail.Trade);
                    if (removed != 0)
                        _queue.Remove(detail);
                    removedCount += removed;
                }
            }
        }

        return removedCount;
    }

    public string[] GetUserList(string format)
    {
        lock (_sync)
            return [.. _queue.Select(z => FormatUser(format, z))];
    }

    private static string FormatUser(string format, TradeEntry<T> z)
        => string.Format(format, z.Trade.Id, z.Trade.Code, z.Trade.Type, z.Username, (Species)z.Trade.TradeData.Species);

    public IReadOnlyList<TradeEntry<T>> GetIsUserQueued(Func<TradeEntry<T>, bool> match)
    {
        lock (_sync)
            return [.. _queue.Where(match)];
    }

    public bool Remove(TradeEntry<T> detail)
    {
        lock (_sync)
        {
            LogUtil.LogInfo($"Removing {detail.Trade.Trainer.TrainerName}", nameof(TradeQueueInfo<>));
            return _queue.Remove(detail);
        }
    }

    public QueueResultAdd IsAbleToJoinQueue(TradeEntry<T> trade, ulong userId, bool sudo = false)
    {
        lock (_sync)
        {
            if (_queue.Any(z => z.UserID == userId) && !sudo)
                return QueueResultAdd.AlreadyInQueue;
            return QueueResultAdd.CanAdd;
        }
    }

    public QueueResultAdd AddToTradeQueue(TradeEntry<T> trade, ulong userId, bool sudo = false)
    {
        lock (_sync)
        {
            // Check again. The check above should immediately precede an Add operation, but ya never know.
            if (_queue.Any(z => z.UserID == userId) && !sudo)
                return QueueResultAdd.AlreadyInQueue;

            // Update the trade data based on settings.
            if (Hub.Config.Legality.ResetHOMETracker && trade.Trade.TradeData is IHomeTrack t)
                t.Tracker = 0;

            // Enqueue with the proper priority.
            var priority = sudo ? PokeTradePriorities.Tier1 : PokeTradePriorities.TierFree;
            var queue = Hub.Queues.GetQueue(trade.Type);
            queue.Enqueue(trade.Trade, priority);
            _queue.Add(trade);

            // Once the trade is finished, remove the user from the list of currently queued users.
            trade.Trade.Notifier.OnFinish = _ => Remove(trade);
            return QueueResultAdd.Added;
        }
    }

    public int GetRandomTradeCode() => Hub.Config.Trade.GetRandomTradeCode();

    public int UserCount(Func<TradeEntry<T>, bool> func)
    {
        lock (_sync)
            return _queue.Count(func);
    }
}
