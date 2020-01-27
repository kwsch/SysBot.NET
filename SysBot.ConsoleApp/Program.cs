using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon;

namespace SysBot.ConsoleApp
{
    internal static class Program
    {
        private const string Path = "config.txt";

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Starting up.");
            if (args.Length != 0)
            {
                Console.WriteLine($"Don't provide any args. Just use {Path}");
                Console.ReadKey();
                return;
            }

            if (!File.Exists(Path))
            {
                Console.WriteLine($"Create {Path}, with IP and Port on separate lines.");
                Console.ReadKey();
                return;
            }

            var lines = File.ReadAllLines("config.txt");
            if (lines.Length < 2)
            {
                Console.WriteLine($"Create {Path}, with IP and Port on separate lines.");
                Console.ReadKey();
                return;
            }
            SwitchBotConfig cfg = new SwitchBotConfig(lines);
            await RunBotAsync(cfg).ConfigureAwait(false);
        }

        private static async Task RunBotAsync(SwitchBotConfig cfg)
        {
            if (cfg.Lines.Length < 4 || !Directory.Exists(cfg.Lines[3]) || !File.Exists(cfg.Lines[2]) || new FileInfo(cfg.Lines[2]).Length != 0x158)
            {
                Console.WriteLine("Needs a valid pk8 path as line 3, and a dump folder as line 4.");
                Console.ReadKey();
                return;
            }

            var data = File.ReadAllBytes(cfg.Lines[2]);
            var pk8 = new PK8(data);

            if (pk8.Species == 0 || !new LegalityAnalysis(pk8).Valid)
            {
                Console.WriteLine("Provided pk8 is not valid.");
                Console.ReadKey();
                return;
            }

            var bot = new SurpriseTradeBot(cfg.IP, cfg.Port);
            bot.InitializeSettings(pk8, cfg.Lines[3]);
            await bot.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
