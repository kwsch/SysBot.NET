using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Pokemon;

namespace SysBot.ConsoleApp
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Starting up.");
            if (args.Length != 0)
            {
                Console.WriteLine("Don't provide any args. Just use the config folder.");
                Console.ReadKey();
                return;
            }
            if (!Directory.Exists("bots"))
            {
                Console.WriteLine("`bots` folder not found.");
                Console.ReadKey();
                return;
            }

            var configs = Directory.EnumerateFiles("bots", "bot*.txt").Select(File.ReadAllLines).ToArray();
            if (configs.Length == 0)
            {
                Console.WriteLine("No configs found. Create a config folder, with each text file having IP and Port on separate lines.");
                Console.ReadKey();
                return;
            }

            if (!File.Exists("config.txt"))
            {
                Console.WriteLine("Config file does not exist.");
                Console.ReadKey();
                return;
            }

            var lines = File.ReadAllText("config.txt");
            if (!int.TryParse(lines, out var choice))
            {
                Console.WriteLine("Config file does not have a supported choice.");
                Console.ReadKey();
                return;
            }

            var task = GetMasterBotTask(choice, configs);
            await task.ConfigureAwait(false);
        }

        private static Task GetMasterBotTask(int choice, string[][] configs)
        {
            var task = choice switch
            {
                1 => DoLinkTradeMulti(configs),
                2 => DoShinyEggFinder(configs),
                _ => DoSurpriseTradeMulti(configs),
            };
            return task;
        }

        private static async Task DoSurpriseTradeMulti(params string[][] lines)
        {
            // Surprise Trade bots. See associated files.
            var token = CancellationToken.None;

            Task[] threads = new Task[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                threads[i] = SurpriseTradeBotUtil.RunBotAsync(lines[i], token);

            await Task.WhenAll(threads).ConfigureAwait(false);
        }

        private static async Task DoLinkTradeMulti(params string[][] lines)
        {
            // Default Bot: Code Trade bots. See associated files.
            var hub = new PokeTradeHub<PK8>();

            var token = CancellationToken.None;

            var first = lines[0];
            Task[] threads = new Task[lines.Length + 1];
            threads[0] = hub.MonitorQueueAddIfEmpty(first[2], token);
            for (int i = 0; i < lines.Length; i++)
                threads[i + 1] = PokeTradeBotUtil.RunBotAsync(lines[i], hub, token);

            await Task.WhenAll(threads).ConfigureAwait(false);
        }

        private static async Task DoShinyEggFinder(params string[][] lines)
        {
            // Shiny Egg receiver bots. See associated files.
            var token = CancellationToken.None;

            Task[] threads = new Task[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                threads[i] = EggBotUtil.RunBotAsync(lines[i], token);

            await Task.WhenAll(threads).ConfigureAwait(false);
        }
    }
}
