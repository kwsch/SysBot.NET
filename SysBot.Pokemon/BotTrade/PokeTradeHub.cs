using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Centralizes logic for trade bot coordination.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="PKM"/> to distribute.</typeparam>
    public class PokeTradeHub<T> where T : PKM, new()
    {
        public PokeTradeHub()
        {
            Barrier = new Barrier(0, ReleaseBarrier);
            BarrierReleasingActions.Add(() =>
                LogUtil.Log(LogLevel.Info, $"{Barrier.ParticipantCount} bots released.", "Barrier"));
        }

        public static readonly PokeTradeLogNotifier<T> LogNotifier = new PokeTradeLogNotifier<T>();

        public PokeTradeHubConfig Config { get; set; } = new PokeTradeHubConfig();

        #region Trade Tracking

        public int CompletedTrades;

        public void AddCompletedTrade()
        {
            Interlocked.Increment(ref CompletedTrades);
            Config.CompletedTrades = CompletedTrades;
        }

        #endregion

        #region Barrier Synchronization
        /// <summary>
        /// Blocks bots from proceeding until all participating bots are waiting at the same step.
        /// </summary>
        public readonly Barrier Barrier;

        public readonly List<Action> BarrierReleasingActions = new List<Action>();
        
        /// <summary>
        /// When the Barrier releases the bots, this method is executed before the bots continue execution.
        /// </summary>
        private void ReleaseBarrier(Barrier b)
        {
            foreach (var action in BarrierReleasingActions)
                action();

            var ms = Config.SynchronizeLinkTradeBotsDelay;
            if (ms != 0)
                Thread.Sleep(ms);
        }
        #endregion

        #region Distribution Queue
        public readonly PokeTradeQueue<T> Queue = new PokeTradeQueue<T>();
        public readonly PokemonPool<T> Pool = new PokemonPool<T>();
        public readonly ConcurrentPool<PokeTradeBot> Bots = new ConcurrentPool<PokeTradeBot>();

        /// <summary>
        /// Spins up a loop that adds a random <see cref="T"/> to the <see cref="Queue"/> if nothing is in it.
        /// </summary>
        /// <param name="path">Folder to randomly distribute from</param>
        /// <param name="token">Thread cancellation</param>
        public async Task MonitorTradeQueueAddIfEmpty(string path, CancellationToken token)
        {
            var blank = new T();
            Pool.ExpectedSize = blank.SIZE_PARTY;
            if (!Pool.LoadFolder(path))
            {
                LogUtil.Log(LogLevel.Error, "Nothing found in pool folder!", "Hub");
            }

            var trainer = new PokeTradeTrainerInfo("Random");
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                if (Queue.Count != 0)
                    continue;
                if (Bots.All(z => z.Config.CurrentRoutineType != PokeRoutineType.LinkTrade))
                    continue;
                if (!Config.DistributeWhileIdle)
                    continue;

                if (Pool.Count == 0)
                    continue;

                var random = Pool.GetRandomPoke();
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
            var blank = new T();
            var size = blank.SIZE_PARTY;

            string queued = Path.Combine(path, "queued");
            string processed = Path.Combine(path, "processed");
            Directory.CreateDirectory(queued);
            Directory.CreateDirectory(processed);

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
                    var processedPath = Path.Combine(queued, Path.GetFileName(f));
                    var finalPath = Path.Combine(processed, Path.GetFileName(f));
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
