using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Centralizes logic for trade bot coordination.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="PKM"/> to distribute.</typeparam>
    public class PokeTradeHub<T> where T : PKM
    {
        #region Trade Tracking
        private int completedTrades;
        public int CompletedTrades => completedTrades;
        public void AddCompletedTrade() => Interlocked.Increment(ref completedTrades);
        #endregion

        #region Trade Codes
        /// <summary>
        /// Minimum trade code to be yielded.
        /// </summary>
        public int MinTradeCode = 8180;

        /// <summary>
        /// Maximum trade code to be yielded.
        /// </summary>
        public int MaxTradeCode = 8199;

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomTradeCode() => Util.Rand.Next(MinTradeCode, MaxTradeCode + 1);
        #endregion

        #region Barrier Synchronization
        /// <summary>
        /// Blocks bots from proceeding until all participating bots are waiting at the same step.
        /// </summary>
        public readonly Barrier Barrier = new Barrier(0, ReleaseBarrier);

        /// <summary>
        /// Toggle to use the <see cref="Barrier"/> during bot operation.
        /// </summary>
        public bool UseBarrier = true;

        /// <summary>
        /// When the Barrier releases the bots, this method is executed before the bots continue execution.
        /// </summary>
        private static void ReleaseBarrier(Barrier b)
        {
            Console.WriteLine($"{b.ParticipantCount} bots released.");
        }
        #endregion

        #region Distribution Queue
        public readonly PokeTradeQueue<T> Queue = new PokeTradeQueue<T>();

        /// <summary>
        /// Spins up a loop that adds a random <see cref="T"/> to the <see cref="Queue"/> if nothing is in it.
        /// </summary>
        /// <param name="path">Folder to randomly distribute from</param>
        /// <param name="token">Thread cancellation</param>
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
