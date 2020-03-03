using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class TradeQueueInfo<T> where T : PKM, new()
    {
        private readonly object _sync = new object();
        private readonly List<TradeEntry<T>> UsersInQueue = new List<TradeEntry<T>>();
        public readonly PokeTradeHub<T> Hub;

        public TradeQueueInfo(PokeTradeHub<T> hub) => Hub = hub;

        public bool ToggleQueue() => Hub.Config.Queues.CanQueue ^= true;
        public bool GetCanQueue() => Hub.Config.Queues.CanQueue && UsersInQueue.Count < Hub.Config.Queues.MaxQueueCount;

        public QueueCheckResult<T> CheckPosition(ulong uid, PokeRoutineType type = 0)
        {
            lock (_sync)
            {
                var index = UsersInQueue.FindIndex(z => z.Equals(uid, type));
                if (index < 0)
                    return QueueCheckResult<T>.None;

                var entry = UsersInQueue[index];
                var actualIndex = 1;
                for (int i = 0; i < index; i++)
                {
                    if (UsersInQueue[i].Type == entry.Type)
                        actualIndex++;
                }

                var inQueue = UsersInQueue.Count(z => z.Type == entry.Type);

                return new QueueCheckResult<T>(true, entry, actualIndex, inQueue);
            }
        }

        public string GetPositionString(ulong uid, PokeRoutineType type = PokeRoutineType.Idle)
        {
            var check = CheckPosition(uid, type);
            if (!check.InQueue || check.Detail is null)
                return "You are not in the queue.";

            var position = $"{check.Position}/{check.QueueCount}";
            var msg = $"You are in the {check.Detail.Type} queue! Position: {position} ({check.Detail.Trade.ID})";
            var pk = check.Detail.Trade.TradeData;
            if (pk.Species != 0)
                msg += $", Receiving: {(Species)check.Detail.Trade.TradeData.Species}";
            return msg;
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

        public QueueResultRemove ClearTrade(ulong userID)
        {
            var details = GetIsUserQueued(userID);
            if (details.Count == 0)
                return QueueResultRemove.NotInQueue;

            int removedCount = ClearTrade(details, Hub);

            if (removedCount == details.Count)
                return QueueResultRemove.Removed;

            foreach (var detail in details)
                Remove(detail);
            return QueueResultRemove.CurrentlyProcessing;
        }

        public int ClearTrade(IEnumerable<TradeEntry<T>> details, PokeTradeHub<T> hub)
        {
            int removedCount = 0;
            lock (_sync)
            {
                var queues = hub.Queues.AllQueues;
                foreach (var detail in details)
                {
                    foreach (var queue in queues)
                    {
                        int removed = queue.Remove(detail.Trade);
                        if (removed != 0)
                            UsersInQueue.Remove(detail);
                        removedCount += removed;
                    }
                }
            }

            return removedCount;
        }

        public IEnumerable<string> GetUserList()
        {
            return UsersInQueue.Select(z => $"({z.Trade.ID}) {z.Username} - {z.Trade.Type} - Code:{z.Trade.Code}");
        }

        public IList<TradeEntry<T>> GetIsUserQueued(ulong userID)
        {
            lock (_sync)
            {
                return UsersInQueue.Where(z => z.UserID == userID).ToArray();
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

                var priority = sudo ? PokeTradeQueue<PK8>.Tier1 : PokeTradeQueue<PK8>.TierFree;
                var queue = Hub.Queues.GetQueue(trade.Type);

                queue.Enqueue(trade.Trade, priority);
                UsersInQueue.Add(trade);

                trade.Trade.Notifier.OnFinish = _ => Remove(trade);
                return QueueResultAdd.Added;
            }
        }

        public int GetRandomTradeCode() => Hub.Config.Trade.GetRandomTradeCode();

        public int Count(Func<TradeEntry<T>, bool> func)
        {
            lock (_sync)
                return UsersInQueue.Count(func);
        }
    }
}