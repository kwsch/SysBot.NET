using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class PokeTradeBotUtil
    {
        public static readonly byte[] EMPTY_EC = new byte[4];
        public static readonly byte[] EMPTY_SLOT = new byte[344];

        /// <summary>
        /// Initializes a <see cref="SysBot"/> connection and starts executing a <see cref="PokeTradeBot"/>.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="queue">Queue to consume from; added to from another thread.</param>
        /// <param name="routine">Task the Trade Bot will perform</param>
        /// <param name="token">Token to indicate cancellation.</param>
        public static async Task RunBotAsync(string[] lines, PokeTradeHub<PK8> queue, PokeTradeRoutine routine, CancellationToken token)
        {
            var bot = CreateNewPokeTradeBot(lines, queue);
            bot.NextRoutine = routine;
            await bot.RunAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a <see cref="SysBot"/> but does not start it.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="hub"></param>
        public static PokeTradeBot CreateNewPokeTradeBot(string[] lines, PokeTradeHub<PK8> hub)
        {
            var cfg = new PokeTradeBotConfig(lines);

            var bot = new PokeTradeBot(hub, cfg);
            if (cfg.DumpFolder != null && Directory.Exists(cfg.DumpFolder))
                bot.DumpFolder = cfg.DumpFolder;
            return bot;
        }
    }
}