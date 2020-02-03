using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon
{
    public static class EggBotUtil
    {
        /// <summary>
        /// Initializes a <see cref="SysBot"/> and starts executing a <see cref="SurpriseTradeBot"/>.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="token">Token to indicate cancellation.</param>
        public static async Task RunBotAsync(string[] lines, CancellationToken token)
        {
            var bot = CreateNewEggBot(lines);
            await bot.RunAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a <see cref="SysBot"/> but does not start it.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        public static EggBot CreateNewEggBot(string[] lines)
        {
            var cfg = new EggBotConfig(lines);

            var bot = new EggBot(cfg);
            if (cfg.DumpFolder != null && Directory.Exists(cfg.DumpFolder))
                bot.DumpFolder = cfg.DumpFolder;
            return bot;
        }
    }
}