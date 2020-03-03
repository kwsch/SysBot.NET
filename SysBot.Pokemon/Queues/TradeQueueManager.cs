using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub;

        private readonly PokeTradeQueue<T> Trade = new PokeTradeQueue<T>(PokeTradeType.Specific);
        private readonly PokeTradeQueue<T> Dudu = new PokeTradeQueue<T>(PokeTradeType.Dudu);
        private readonly PokeTradeQueue<T> Clone = new PokeTradeQueue<T>(PokeTradeType.Clone);
        private readonly PokeTradeQueue<T> Dump = new PokeTradeQueue<T>(PokeTradeType.Dump);
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

        public void ClearAll()
        {
            foreach (var q in AllQueues)
                q.Clear();
        }

        public bool TryDequeueLedy(out PokeTradeDetail<T> detail)
        {
            detail = default!;
            var cfg = Hub.Config.Distribute;
            if (!cfg.DistributeWhileIdle)
                return false;

            if (Hub.Ledy.Pool.Count == 0)
                return false;

            var random = Hub.Ledy.Pool.GetRandomPoke();
            var code = cfg.TradeCode;
            var trainer = new PokeTradeTrainerInfo("Random Distribution");
            detail = new PokeTradeDetail<T>(random, trainer, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code);
            return true;
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