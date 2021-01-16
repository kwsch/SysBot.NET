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
            if (args.Length > 1)
                Console.WriteLine("This program does not support command line arguments.");

            if (!File.Exists(ConfigPath))
            {
                ExitNoConfig();
                return;
            }

            try
            {
                var lines = File.ReadAllText(ConfigPath);
                var cfg = JsonConvert.DeserializeObject<ProgramConfig>(lines);
                RunBots(cfg);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.WriteLine("Unable to start bots with saved config file. Please copy your config from the WinForms project or delete it and reconfigure.");
                Console.ReadKey();
            }
        }

        private static void ExitNoConfig()
        {
            var bot = new PokeBotState { Connection = new SwitchConnectionConfig { IP = "192.168.0.1", Port = 6000 }, InitialRoutine = PokeRoutineType.FlexTrade };
            var cfg = new ProgramConfig { Bots = new[] { bot } };
            var created = JsonConvert.SerializeObject(cfg, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore
            });
            File.WriteAllText(ConfigPath, created);
            Console.WriteLine("Created new config file since none was found in the program's path. Please configure it and restart the program.");
            Console.WriteLine("It is suggested to configure this config file using the GUI project if possible, as it will help you assign values correctly.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void RunBots(ProgramConfig prog)
        {
            var env = new PokeBotRunnerImpl(prog.Hub);
            foreach (var bot in prog.Bots)
            {
                bot.Initialize();
                if (!AddBot(env, bot))
                    Console.WriteLine($"Failed to add bot: {bot}");
            }

            PokeTradeBot.SeedChecker = new Z3SeedSearchHandler<PK8>();
            LogUtil.Forwarders.Add((msg, ident) => Console.WriteLine($"{ident}: {msg}"));
            env.StartAll();
            Console.WriteLine($"Started all bots (Count: {prog.Bots.Length}.");
            Console.WriteLine("Press any key to stop execution and quit. Feel free to minimize this window!");
            Console.ReadKey();
            env.StopAll();
        }

        private static bool AddBot(PokeBotRunnerImpl env, PokeBotState cfg)
        {
            if (!cfg.IsValid())
            {
                Console.WriteLine($"{cfg}'s config is not valid.");
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

            Console.WriteLine($"Added: {cfg}: {cfg.InitialRoutine}");
            return true;
        }
    }
}
