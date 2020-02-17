using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class TradeQueueInfo<T> where T : PKM, new()
    {
        private readonly object _sync = new object();
        private readonly List<TradeEntry<T>> UsersInQueue = new List<TradeEntry<T>>();

        public PokeTradeHub<T> Hub { get; set; } = new PokeTradeHub<T>(new PokeTradeHubConfig());
        public bool CanQueue { get; set; } = true;

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

                return new QueueCheckResult<T>(true, entry, actualIndex);
            }
        }

        public string GetTradeList()
        {
            lock (_sync)
            {
                if (UsersInQueue.Count == 0)
                    return "Nobody in any queue.";

                var queued = UsersInQueue.GroupBy(z => z.Type);
                var list = queued.SelectMany(z => z.Select(x =>
                    $"{x.Type}: {x.Trade.Trainer.TrainerName} ({x.Username}), {(Species)x.Trade.TradeData.Species}"));
                return string.Join("\n", list);
            }
        }

        public void ClearAllQueues()
        {
            lock (_sync)
            {
                Hub.Dudu.Clear();
                Hub.Queue.Clear();
                UsersInQueue.Clear();
            }
        }

        public QueueResultRemove ClearTrade(ulong userID)
        {
            var details = GetIsUserQueued(userID);
            if (details.Count == 0)
                return QueueResultRemove.NotInQueue;

            int removedCount = ClearTrade(details, Hub);

            if (removedCount != details.Count)
                return QueueResultRemove.CurrentlyProcessing;

            return QueueResultRemove.Removed;
        }

        public int ClearTrade(IEnumerable<TradeEntry<T>> details, PokeTradeHub<T> hub)
        {
            int removedCount = 0;
            lock (_sync)
            {
                foreach (var detail in details)
                {
                    int removed = hub.Queue.Remove(detail.Trade);
                    if (removed != 0)
                        UsersInQueue.Remove(detail);
                    removedCount += removed;

                    removed = hub.Dudu.Remove(detail.Trade);
                    if (removed != 0)
                        UsersInQueue.Remove(detail);
                    removedCount += removed;
                }
            }

            return removedCount;
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
                return UsersInQueue.Remove(detail);
        }

        public QueueResultAdd AddToTradeQueue(TradeEntry<T> trade, ulong userID, bool sudo = false)
        {
            lock (_sync)
            {
                if (UsersInQueue.Any(z => z.UserID == userID) && !sudo)
                    return QueueResultAdd.AlreadyInQueue;

                if (Hub.Config.ResetHOMETracker && trade.Trade.TradeData is IHomeTrack t)
                    t.Tracker = 0;

                var priority = sudo ? PokeTradeQueue<PK8>.Tier1 : PokeTradeQueue<PK8>.TierFree;
                var queue = Hub.GetQueue(trade.Type);

                queue.Enqueue(trade.Trade, priority);
                UsersInQueue.Add(trade);

                trade.Trade.Notifier.OnFinish = r =>
                {
                    r.Connection.Log($"Removing {trade.Username}'s request from the queue.");
                    Remove(trade);
                };
                return QueueResultAdd.Added;
            }
        }

        public int GetRandomTradeCode()
        {
            var cfg = Hub.Config;
            return Util.Rand.Next(cfg.MinTradeCode, cfg.MaxTradeCode + 1);
        }
    }

    public enum QueueResultRemove
    {
        Removed,
        CurrentlyProcessing,
        NotInQueue,
    }

    public enum QueueResultAdd
    {
        Added,
        AlreadyInQueue,
    }

    public class QueueCheckResult<T> where T : PKM, new()
    {
        public readonly bool InQueue;
        public readonly TradeEntry<T>? Detail;
        public readonly int Position;

        public static readonly QueueCheckResult<T> None = new QueueCheckResult<T>();

        public QueueCheckResult(bool inQueue = false, TradeEntry<T>? detail = default, int position = -1)
        {
            InQueue = inQueue;
            Detail = detail;
            Position = position;
        }
    }
}