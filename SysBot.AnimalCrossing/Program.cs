using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace SysBot.AnimalCrossing
{
    internal static class Program
    {
        private const string ConfigPath = "config.json";

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Starting up...");
            if (args.Length > 1)
                Console.WriteLine("This program does not support command line arguments.");

            if (!File.Exists(ConfigPath))
            {
                CreateConfigQuit();
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<CrossBotConfig>(json);
            SaveConfig(config);

            var bot = new CrossBot(config);

            var cancel = CancellationToken.None;
            var sys = new SysCord(bot);

            Globals.Self = sys;
            Globals.Bot = bot;

            Console.WriteLine("Starting Discord.");
#pragma warning disable 4014
            Task.Run(() => sys.MainAsync(config.Token, cancel), cancel);
#pragma warning restore 4014

            Console.WriteLine("Starting bot loop.");
            var task = bot.RunAsync(cancel);
            await task;
            if (task.IsFaulted)
            {
                if (task.Exception == null)
                {
                    Console.WriteLine("Bot has terminated due to an unknown error.");
                }
                else
                {
                    Console.WriteLine("Bot has terminated due to an error:");
                    foreach (var ex in task.Exception.InnerExceptions)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
            else
            {
                Console.WriteLine("Bot has terminated.");
            }

            Console.WriteLine("Press any key to exit.");
        }

        private static void SaveConfig(CrossBotConfig config)
        {
            var options = new JsonSerializerOptions {WriteIndented = true};
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigPath, json);
        }

        private static void CreateConfigQuit()
        {
            SaveConfig(new CrossBotConfig {IP = "192.168.0.1", Port = 6000});
            Console.WriteLine("Created blank config file. Please configure it and restart the program.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
