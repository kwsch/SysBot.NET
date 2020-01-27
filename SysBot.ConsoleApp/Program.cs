using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

            // Default Bot: Surprise Trade bot. See associated files.
            await SurpriseTradeBotUtil.RunBotAsync(lines, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
