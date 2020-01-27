using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon
{
    public static class SurpriseTradeBotUtil
    {
        /// <summary>
        /// Initializes a <see cref="SysBot"/> and starts executing a <see cref="SurpriseTradeBot"/>.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="token">Token to indicate cancellation.</param>
        public static async Task RunBotAsync(string[] lines, CancellationToken token)
        {
            var bot = CreateNewSurpriseTradeBot(lines);
            await bot.RunAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a <see cref="SysBot"/> but does not start it.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        public static SurpriseTradeBot CreateNewSurpriseTradeBot(string[] lines)
        {
            var cfg = new PokeDistributionBotConfig(lines);
            if (cfg.DistributeFolder == null || !Directory.Exists(cfg.DistributeFolder))
                throw new ArgumentNullException(nameof(cfg.DistributeFolder), "Needs a valid source folder.");

            var bot = new SurpriseTradeBot(cfg);
            if (!bot.LoadFolder(cfg.DistributeFolder))
                throw new ArgumentNullException(nameof(cfg.DistributeFolder), "Failed to load anything legal.");

            if (cfg.DumpFolder != null && Directory.Exists(cfg.DumpFolder))
                bot.DumpFolder = cfg.DumpFolder;
            return bot;
        }
    }
}
