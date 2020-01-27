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
            if (cfg.Lines.Length < 3 || !Directory.Exists(cfg.Lines[2]))
            {
                Console.WriteLine("Needs a valid source folder. The 4th line is an optional dump folder.");
                Console.ReadKey();
                return;
            }

            var bot = new SurpriseTradeBot(cfg.IP, cfg.Port);
            if (!bot.LoadFolder(cfg.Lines[2]))
            {
                Console.WriteLine("Failed to load anything legal.");
                Console.ReadKey();
                return;
            }

            if (cfg.Lines.Length < 4 && Directory.Exists(cfg.Lines[3]))
                bot.DumpFolder = cfg.Lines[3];

            await bot.RunAsync(token).ConfigureAwait(false);
        }
    }
}
