using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon;

/// <summary>
/// Contains a queue of users to be processed.
/// </summary>
/// <typeparam name="T">Type of data to be transmitted to the users</typeparam>
public sealed record TradeQueueInfo<T>(PokeTradeHub<T> Hub)
    where T : PKM, new()
{
    private readonly object _sync = new();
    private readonly List<TradeEntry<T>> UsersInQueue = [];
    public readonly PokeTradeHub<T> Hub = Hub;
    private readonly TradeCodeStorage _tradeCodeStorage = new();

    public bool IsUserInQueue(ulong userId)
    {
        lock (_sync)
        {
            return UsersInQueue.Any(entry => entry.UserID == userId);
        }
    }

    public int Count
    {
        get
        {
            lock (_sync)
                return UsersInQueue.Count;
        }
    }

    public bool ToggleQueue() => Hub.Config.Queues.CanQueue ^= true;

    public bool GetCanQueue()
    {
        if (!Hub.Config.Queues.CanQueue)
            return false;
        lock (_sync)
            return UsersInQueue.Count < Hub.Config.Queues.MaxQueueCount && Hub.TradeBotsReady;
    }

    public TradeEntry<T>? GetDetail(ulong uid)
    {
        lock (_sync)
            return UsersInQueue.Find(z => z.UserID == uid);
    }

    public QueueCheckResult<T> CheckPosition(ulong uid, int uniqueTradeID, PokeRoutineType type = 0)
    {
        lock (_sync)
        {
            var allTrades = Hub.Queues.AllQueues.SelectMany(q => q.Queue.Select(x => x.Value)).ToList();
            var index = allTrades.FindIndex(z => z.Trainer.ID == uid && z.UniqueTradeID == uniqueTradeID);
            if (index < 0)
                return QueueCheckResult<T>.None;

            var entry = allTrades[index];
            var actualIndex = index + 1;

            var inQueue = allTrades.Count;

            return new QueueCheckResult<T>(true, new TradeEntry<T>(entry, uid, type, entry.Trainer.TrainerName, uniqueTradeID), actualIndex, inQueue);
        }
    }

    public string GetPositionString(ulong uid, int uniqueTradeID, PokeRoutineType type = PokeRoutineType.Idle)
    {
        var check = CheckPosition(uid, uniqueTradeID, type);
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
            UsersInQueue.Clear();
        }
    }

    public QueueResultRemove ClearTrade(string userName)
    {
        var details = GetIsUserQueued(z => z.Username == userName);
        return ClearTrade(details);
    }

    public QueueResultRemove ClearTrade(ulong userID)
    {
        var details = GetIsUserQueued(z => z.UserID == userID);
        return ClearTrade(details);
    }

    private QueueResultRemove ClearTrade(ICollection<TradeEntry<T>> details)
    {
        if (details.Count == 0)
            return QueueResultRemove.NotInQueue;

        bool removedAll = true;
        bool currentlyProcessing = false;
        bool removedPending = false;

        foreach (var detail in details)
        {
            if (detail.Trade.IsProcessing)
            {
                currentlyProcessing = true;
                if (!Hub.Config.Queues.CanDequeueIfProcessing)
                {
                    removedAll = false;
                    continue;
                }
            }
            else
            {
                if (Remove(detail))
                    removedPending = true;
            }
        }

        if (!removedAll && currentlyProcessing && !removedPending)
            return QueueResultRemove.CurrentlyProcessing;

        if (currentlyProcessing && removedPending)
            return QueueResultRemove.CurrentlyProcessingRemoved;

        if (removedPending)
            return QueueResultRemove.Removed;

        return QueueResultRemove.NotInQueue;
    }

    public IEnumerable<string> GetUserList(string fmt)
    {
        lock (_sync)
        {
            return UsersInQueue.Select(z => string.Format(fmt, z.Trade.ID, z.Trade.Code, z.Trade.Type, z.Username, (Species)z.Trade.TradeData.Species));
        }
    }

    public IEnumerable<ulong> GetUserIdList(int count)
    {
        lock (_sync)
        {
            return UsersInQueue.Take(count).Select(z => z.Trade.Trainer.ID);
        }
    }

    public IList<TradeEntry<T>> GetIsUserQueued(Func<TradeEntry<T>, bool> match)
    {
        lock (_sync)
        {
            return UsersInQueue.Where(match).ToArray();
        }
    }

    public bool Remove(TradeEntry<T> detail)
    {
        lock (_sync)
        {
            LogUtil.LogInfo($"Removing {detail.Trade.Trainer.TrainerName}", nameof(TradeQueueInfo<T>));
            return UsersInQueue.Remove(detail);
        }
    }

    public QueueResultAdd AddToTradeQueue(TradeEntry<T> trade, ulong userID, bool sudo = false)
    {
        lock (_sync)
        {
            if (UsersInQueue.Any(z => z.UserID == userID) && !sudo)
                return QueueResultAdd.AlreadyInQueue;

            if (Hub.Config.Legality.ResetHOMETracker && trade.Trade.TradeData is IHomeTrack t)
                t.Tracker = 0;

            var priority = sudo ? PokeTradePriorities.Tier1 : PokeTradePriorities.TierFree;
            var queue = Hub.Queues.GetQueue(trade.Type);

            queue.Enqueue(trade.Trade, priority);
            UsersInQueue.Add(trade);

            trade.Trade.Notifier.OnFinish = _ => Remove(trade);
            return QueueResultAdd.Added;
        }
    }

    public int GetRandomTradeCode(ulong trainerID)
    {
        if (Hub.Config.Trade.TradeConfiguration.StoreTradeCodes)
        {
            return _tradeCodeStorage.GetTradeCode(trainerID);
        }
        else
        {
            return Hub.Config.Trade.GetRandomTradeCode();
        }
    }

    public List<Pictocodes> GetRandomLGTradeCode()
    {
        var code = new List<Pictocodes>();
        for (int i = 0; i <= 2; i++)
        {
            code.Add((Pictocodes)Util.Rand.Next(10));
            code.Add(Pictocodes.Pikachu);

        }
        return code;
    }

    public int UserCount(Func<TradeEntry<T>, bool> func)
    {
        lock (_sync)
            return UsersInQueue.Count(func);
    }
}
