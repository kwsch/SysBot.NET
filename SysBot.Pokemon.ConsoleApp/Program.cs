using System;
using System.IO;
using Newtonsoft.Json;
using SysBot.Base;

namespace SysBot.Pokemon.ConsoleApp
{
    public static class Program
    {
        private const string ConfigPath = "config.json";

        private static void Main(string[] args)
        {
            Console.WriteLine("Starting up...");
            if (args.Length > 1)
                Console.WriteLine("This program does not support command line arguments.");

            if (File.Exists(ConfigPath))
            {
                var lines = File.ReadAllText(ConfigPath);
                var prog = JsonConvert.DeserializeObject<ProgramConfig>(lines);
                var env = new PokeBotRunnerImpl(prog.Hub);
                foreach (var bot in prog.Bots)
                {
                    bot.Initialize();
                    AddBot(bot);
                }

                LogUtil.Forwarders.Add((msg, ident) => Console.WriteLine($"{ident}: {msg}"));
                env.StartAll();
                Console.WriteLine("Started all bots.");
                Console.WriteLine("Press any key to stop execution and quit.");
                Console.ReadKey();
                env.StopAll();
            }
            else
            {
                Console.WriteLine("Unable to parse config file. Please copy your config from the WinForms project.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void AddBot(PokeBotConfig bot)
        {
            Console.WriteLine($"Added: {bot.IP}: {bot.InitialRoutine}");
        }
    }
}
