using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        public readonly PokeTradeHub<T> Hub;

        public readonly PokeTradeQueue<T> Trade = new PokeTradeQueue<T>(PokeTradeType.Specific);
        public readonly PokeTradeQueue<T> Dudu = new PokeTradeQueue<T>(PokeTradeType.Dudu);
        public readonly PokeTradeQueue<T> Clone = new PokeTradeQueue<T>(PokeTradeType.Clone);
        public readonly PokeTradeQueue<T> Dump = new PokeTradeQueue<T>(PokeTradeType.Dump);
        public readonly TradeQueueInfo<T> Info;
        public readonly PokeTradeQueue<T>[] AllQueues;

        public TradeQueueManager(PokeTradeHub<T> hub)
        {
            Hub = hub;
            Info = new TradeQueueInfo<T>(hub);
            AllQueues = new[] { Dudu, Dump, Clone, Trade, };
        }

        public PokeTradeQueue<T> GetQueue(PokeRoutineType type)
        {
            return type switch
            {
                PokeRoutineType.DuduBot => Dudu,
                PokeRoutineType.Clone => Clone,
                PokeRoutineType.Dump => Dump,
                _ => Trade,
            };
        }

        /// <summary>
        /// Spins up a loop that adds a random <see cref="T"/> to the <see cref="Trade"/> if nothing is in it.
        /// </summary>
        /// <param name="path">Folder to randomly distribute from</param>
        /// <param name="token">Thread cancellation</param>
        public async Task MonitorTradeQueueAddIfEmpty(string path, CancellationToken token)
        {
            if (!Hub.Ledy.Pool.LoadFolder(path))
                LogUtil.LogError("Nothing found in pool folder!", "Hub");

            var trainer = new PokeTradeTrainerInfo("Random");
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                if (Trade.Count != 0)
                    continue;
                if (Hub.Bots.All(z => (z.Config.CurrentRoutineType != PokeRoutineType.LinkTrade && z.Config.CurrentRoutineType != PokeRoutineType.FlexTrade)))
                    continue;

                var cfg = Hub.Config.Distribute;
                if (!cfg.DistributeWhileIdle)
                    continue;

                if (Hub.Ledy.Pool.Count == 0)
                    continue;

                var random = Hub.Ledy.Pool.GetRandomPoke();
                var code = cfg.TradeCode;
                var detail = new PokeTradeDetail<T>(random, trainer, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code);
                Trade.Enqueue(detail);
            }
        }

        public void ClearAll()
        {
            foreach (var q in AllQueues)
                q.Clear();
        }

        public bool TryDequeue(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
        {
            if (type == PokeRoutineType.FlexTrade)
                return GetFlexDequeue(out detail, out priority);

            return TryDequeueInternal(type, out detail, out priority);
        }

        private bool TryDequeueInternal(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
        {
            var queue = GetQueue(type);
            return queue.TryDequeue(out detail, out priority);
        }

        private bool GetFlexDequeue(out PokeTradeDetail<T> detail, out uint priority)
        {
            var cfg = Hub.Config.Queues;
            if (cfg.FlexMode == FlexYieldMode.LessCheatyFirst)
                return GetFlexDequeueOld(out detail, out priority);
            return GetFlexDequeueWeighted(cfg, out detail, out priority);
        }

        private bool GetFlexDequeueWeighted(QueueSettings cfg, out PokeTradeDetail<T> detail, out uint priority)
        {
            PokeTradeQueue<T>? preferredQueue = null;
            long bestWeight = 0;
            uint bestPriority = 0;
            foreach (var q in AllQueues)
            {
                var peek = q.TryPeek(out detail, out priority);
                if (!peek)
                    continue;

                if (priority < bestPriority)
                    continue;

                var count = q.Count;
                var time = detail.Time;
                var weight = cfg.GetWeight(count, time, q.Type);

                if (priority <= bestPriority && weight <= bestWeight)
                    continue; // not good enough to be preferred over the other.

                bestWeight = weight;
                bestPriority = priority;
                preferredQueue = q;
            }

            if (preferredQueue == null)
            {
                detail = default!;
                priority = default;
                return false;
            }

            return preferredQueue.TryDequeue(out detail, out priority);
        }

        private bool GetFlexDequeueOld(out PokeTradeDetail<T> detail, out uint priority)
        {
            if (TryDequeueInternal(PokeRoutineType.DuduBot, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.Clone, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.Dump, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.LinkTrade, out detail, out priority))
                return true;
            return false;
        }

        public void Enqueue(PokeRoutineType type, PokeTradeDetail<T> detail, uint priority)
        {
            var queue = GetQueue(type);
            queue.Enqueue(detail, priority);
        }
    }
}