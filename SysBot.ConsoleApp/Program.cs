using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Pokemon;

namespace SysBot.ConsoleApp
{
    internal static class Program
    {
        private const string PathSurprise = "Surprise";
        private const string PathLinkCode = "LinkCode";
        private const string PathShinyEgg = "ShinyEgg";

        private static async Task Main(string[] args)
        {
            if (args.Length > 1)
                await LaunchViaArgs(args).ConfigureAwait(false);
            else
                await LaunchWithoutArgs().ConfigureAwait(false);

            Console.WriteLine("No bots are currently running. Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task LaunchViaArgs(string[] args)
        {
            Console.WriteLine("Starting up single-bot environment from provided arguments.");
            var BotTypes = typeof(Program).GetFields(BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static)
                .Where(z => z.Name.StartsWith("Path"))
                .Select(z => z.GetRawConstantValue()).ToArray();
            // Launch a single bot.
            var type = args[1];
            var config = args[2];
            var lines = File.ReadAllLines(config);
            var task = GetBotsWithConfigs(Array.IndexOf(BotTypes, type), new[] { lines });
            await task.ConfigureAwait(false);
        }

        private static async Task LaunchWithoutArgs()
        {
            Console.WriteLine("Starting up multi-bot environment.");
            var task0 = GetBotTask(PathSurprise, 0, out var count0);
            var task1 = GetBotTask(PathLinkCode, 1, out var count1);
            var task2 = GetBotTask(PathShinyEgg, 2, out var count2);

            int botCount = count0 + count1 + count2;

            if (botCount == 0)
            {
                Console.WriteLine("No bots started. Verify folder configs.");
                return;
            }

            var tasks = new[] { task0, task1, task2 };
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static Task GetBotTask(string path, int botType, out int count)
        {
            Directory.CreateDirectory(path);
            var files = Directory.GetFiles(path, "bot*.txt", SearchOption.TopDirectoryOnly);
            count = files.Length;
            if (count == 0)
                return Task.CompletedTask;

            var configs = files.Select(File.ReadAllLines).ToArray();

            Console.WriteLine($"Found {count} config(s) in {path}. Creating bot(s)...");
            return GetBotsWithConfigs(botType, configs);
        }

        private static Task GetBotsWithConfigs(int botType, string[][] configs)
        {
            return botType switch
            {
                1 => DoLinkTradeMulti(configs),
                2 => DoShinyEggFinder(configs),
                _ => DoSurpriseTradeMulti(configs),
            };
        }

        private static async Task DoSurpriseTradeMulti(params string[][] lines)
        {
            // Surprise Trade bots. See associated files.
            var token = CancellationToken.None;
            Console.WriteLine($"Creating {lines.Length} bot(s) for Surprise Trades.");

            Task[] threads = new Task[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                threads[i] = SurpriseTradeBotUtil.RunBotAsync(lines[i], token);

            await Task.WhenAll(threads).ConfigureAwait(false);
        }

        private static async Task DoLinkTradeMulti(params string[][] lines)
        {
            // Default Bot: Code Trade bots. See associated files.
            var token = CancellationToken.None;

            var first = lines[0];
            var hubRandomPath = first[2];
            Console.WriteLine($"Creating a hub for {lines.Length} bot(s) with random distribution from the following path: {hubRandomPath}");
            var hub = new PokeTradeHub<PK8>();

            Task[] threads = new Task[lines.Length + 1];
            threads[0] = hub.MonitorQueueAddIfEmpty(hubRandomPath, token);
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
