using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class PokeTradeBotUtil
    {
        /// <summary>
        /// Initializes a <see cref="SysBot"/> and starts executing a <see cref="SurpriseTradeBot"/>.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="queue">Queue to consume from; added to from another thread.</param>
        /// <param name="token">Token to indicate cancellation.</param>
        public static async Task RunBotAsync(string[] lines, PokeTradeQueue<PK8> queue, CancellationToken token)
        {
            var bot = CreateNewPokeTradeBot(lines, queue);
            await bot.RunAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a <see cref="SysBot"/> but does not start it.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="queue"></param>
        public static PokeTradeBot CreateNewPokeTradeBot(string[] lines, PokeTradeQueue<PK8> queue)
        {
            var cfg = new PokeTradeBotConfig(lines);

            var bot = new PokeTradeBot(queue, cfg);
            if (cfg.DumpFolder != null && Directory.Exists(cfg.DumpFolder))
                bot.DumpFolder = cfg.DumpFolder;
            return bot;
        }

        public static async Task MonitorQueueAddIfEmpty<T>(PokeTradeQueue<T> queue, string path, CancellationToken token) where T : PKM
        {
            var blank = (T)Activator.CreateInstance(typeof(T));
            var pool = new PokemonPool<T> { ExpectedSize = blank.SIZE_PARTY };
            pool.LoadFolder(path);

            var trainer = new PokeTradeTrainerInfo("Random");
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(10_000, token).ConfigureAwait(false);
                if (queue.Count != 0)
                    continue;

                var random = pool.GetRandomPoke();
                var detail = new PokeTradeDetail<T>(random, trainer);
                queue.Enqueue(detail);
            }
        }
    }
}