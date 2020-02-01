using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokeTradeHub<T> where T : PKM
    {
        public readonly PokeTradeQueue<T> Queue = new PokeTradeQueue<T>();
        public readonly Barrier Barrier = new Barrier(0, ReleaseBarrier);
        public bool UseBarrier = true;

        private int completedTrades;
        public int CompletedTrades => completedTrades;
        public void AddCompletedTrade() => Interlocked.Increment(ref completedTrades);

        private static void ReleaseBarrier(Barrier obj)
        {
            Console.WriteLine($"{obj.ParticipantCount} bots released.");
        }

        public async Task MonitorQueueAddIfEmpty(string path, CancellationToken token)
        {
            var blank = (T)Activator.CreateInstance(typeof(T));
            var pool = new PokemonPool<T> { ExpectedSize = blank.SIZE_PARTY };
            pool.LoadFolder(path);

            var trainer = new PokeTradeTrainerInfo("Random");
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10_000, token).ConfigureAwait(false);
                if (Queue.Count != 0)
                    continue;

                var random = pool.GetRandomPoke();
                var detail = new PokeTradeDetail<T>(random, trainer);
                Queue.Enqueue(detail);
            }
        }
    }
}
