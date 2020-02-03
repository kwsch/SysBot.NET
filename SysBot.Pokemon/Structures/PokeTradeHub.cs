using System;
using System.IO;
using System.Text;
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
        public static readonly PokeTradeLogNotifier<T> LogNotifier = new PokeTradeLogNotifier<T>();

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
                var detail = new PokeTradeDetail<T>(random, trainer, LogNotifier);
                Queue.Enqueue(detail);
            }
        }

        /// <summary>
        /// Spins up a loop that adds a specified <see cref="T"/> to the <see cref="Queue"/> to carry out the trade.
        /// </summary>
        /// <param name="path">Folder to acquire new trades from</param>
        /// <param name="notifier">Object that processes notifications when the trade is being executed</param>
        /// <param name="token">Thread cancellation</param>
        public async Task MonitorFolderAddPriority(string path, IPokeTradeNotifier<T> notifier, CancellationToken token)
        {
            var blank = (T)Activator.CreateInstance(typeof(T));
            var size = blank.SIZE_PARTY;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(5_000, token).ConfigureAwait(false);
                var files = Directory.EnumerateFiles(path);
                foreach (var f in files)
                {
                    var data = File.ReadAllBytes(f);
                    if (data.Length < size + 10)
                        continue;

                    var pkmData = data.Slice(0, size);
                    var pkm = PKMConverter.GetPKMfromBytes(pkmData);
                    if (!(pkm is T t))
                        continue;

                    var priority = BitConverter.ToUInt32(data, size);
                    var code = BitConverter.ToInt32(data, size + 4);
                    var name = Encoding.Unicode.GetString(data, size + 8, data.Length - (size + 8));
                    var trainer = new PokeTradeTrainerInfo(name);

                    // Move to subfolder as it is processed.
                    var processedPath = Path.Combine(path, "loaded", Path.GetFileName(f));
                    var finalPath = Path.Combine(path, "loaded", Path.GetFileName(f));
                    File.Move(f, processedPath);

                    var detail = new PokeTradeDetail<T>(t, trainer, notifier, code)
                    {
                        SourcePath = processedPath,
                        DestinationPath = finalPath,
                    };

                    Queue.Enqueue(detail, priority);
                }
            }
        }
        #endregion
    }
}
