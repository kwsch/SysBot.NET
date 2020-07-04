using System;
using System.IO;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.ConsoleApp
{
    public static class Program
    {
        private const string ConfigPath = "config.json";

        private static void Main(string[] args)
        {
            Console.WriteLine("Starting up...");
            PokeTradeBot.SeedChecker = new Z3SeedSearchHandler<PK8>();
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
                    if (!AddBot(env, bot))
                        Console.WriteLine($"Failed to add bot: {bot.IP}");
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

        private static bool AddBot(PokeBotRunner env, PokeBotConfig cfg)
        {
            if (!cfg.IsValidIP())
            {
                Console.WriteLine($"{cfg.IP}'s config is not valid.");
                return false;
            }

            var newbot = env.CreateBotFromConfig(cfg);
            try
            {
                env.Add(newbot);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            Console.WriteLine($"Added: {cfg.IP}: {cfg.InitialRoutine}");
            return true;
        }
    }
}
