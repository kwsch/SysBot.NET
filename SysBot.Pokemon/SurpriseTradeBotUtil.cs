using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon
{
    public static class SurpriseTradeBotUtil
    {
        public static async Task RunBotAsync(string[] lines, CancellationToken token)
        {
            PokeDistributionBotConfig cfg = new PokeDistributionBotConfig(lines);
            if (cfg.DistributeFolder == null || !Directory.Exists(cfg.DistributeFolder))
            {
                Console.WriteLine("Needs a valid source folder.");
                Console.ReadKey();
                return;
            }

            var bot = new SurpriseTradeBot(cfg);
            if (!bot.LoadFolder(cfg.DistributeFolder))
            {
                Console.WriteLine("Failed to load anything legal.");
                Console.ReadKey();
                return;
            }

            if (cfg.DumpFolder != null && Directory.Exists(cfg.DumpFolder))
                bot.DumpFolder = cfg.DumpFolder;

            await bot.RunAsync(token).ConfigureAwait(false);
        }
    }
}
