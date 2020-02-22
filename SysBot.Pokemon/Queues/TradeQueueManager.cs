using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        public readonly PokeTradeHub<T> Hub;

        public readonly PokeTradeQueue<T> Queue = new PokeTradeQueue<T>();
        public readonly PokeTradeQueue<T> Dudu = new PokeTradeQueue<T>();
        public readonly TradeQueueInfo<T> Info;

        public TradeQueueManager(PokeTradeHub<T> hub)
        {
            Hub = hub;
            Info = new TradeQueueInfo<T>(hub);
        }

        public PokeTradeQueue<T> GetQueue(PokeRoutineType type)
        {
            return type switch
            {
                PokeRoutineType.DuduBot => Dudu,
                _ => Queue,
            };
        }

        /// <summary>
        /// Spins up a loop that adds a random <see cref="T"/> to the <see cref="Queue"/> if nothing is in it.
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
                if (Queue.Count != 0)
                    continue;
                if (Hub.Bots.All(z => z.Config.CurrentRoutineType != PokeRoutineType.LinkTrade))
                    continue;
                if (!Hub.Config.DistributeWhileIdle)
                    continue;

                if (Hub.Ledy.Pool.Count == 0)
                    continue;

                var random = Hub.Ledy.Pool.GetRandomPoke();
                var detail = new PokeTradeDetail<T>(random, trainer, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random);
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

                    var detail = new PokeTradeDetail<T>(t, trainer, notifier, PokeTradeType.Specific, code)
                    {
                        SourcePath = processedPath,
                        DestinationPath = finalPath,
                    };

                    Queue.Enqueue(detail, priority);
                }
            }
        }

        public void ClearAll()
        {
            Dudu.Clear();
            Queue.Clear();
        }
    }
}