using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokeTradeHub<T> where T : PKM
    {
        #region Trade Tracking
        private int completedTrades;
        public int CompletedTrades => completedTrades;
        public void AddCompletedTrade() => Interlocked.Increment(ref completedTrades);
        #endregion

        #region Trade Codes
        public int MaxTradeCode;
        public int MinTradeCode;

        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
        #endregion

        #region Barrier Synchronization
        public readonly Barrier Barrier = new Barrier(0, ReleaseBarrier);
        public bool UseBarrier = true;

        private static void ReleaseBarrier(Barrier obj)
        {
            Console.WriteLine($"{obj.ParticipantCount} bots released.");
        }
        #endregion

        #region Distribution Queue
        public readonly PokeTradeQueue<T> Queue = new PokeTradeQueue<T>();

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
        #endregion
    }
}
