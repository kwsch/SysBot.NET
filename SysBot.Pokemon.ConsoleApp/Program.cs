using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Z3;

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
                var cfg = JsonSerializer.Deserialize(lines, ProgramConfigContext.Default.ProgramConfig) ?? new ProgramConfig();
                PokeTradeBot.SeedChecker = new Z3SeedSearchHandler<PK8>();
                BotContainer.RunBots(cfg);
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to start bots with saved config file. Please copy your config from the WinForms project or delete it and reconfigure.");
                Console.ReadKey();
            }
        }

        private static void ExitNoConfig()
        {
            var bot = new PokeBotState { Connection = new SwitchConnectionConfig { IP = "192.168.0.1", Port = 6000 }, InitialRoutine = PokeRoutineType.FlexTrade };
            var cfg = new ProgramConfig { Bots = new[] { bot } };
            var created = JsonSerializer.Serialize(cfg, ProgramConfigContext.Default.ProgramConfig);
            File.WriteAllText(ConfigPath, created);
            Console.WriteLine("Created new config file since none was found in the program's path. Please configure it and restart the program.");
            Console.WriteLine("It is suggested to configure this config file using the GUI project if possible, as it will help you assign values correctly.");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }

    [JsonSerializable(typeof(ProgramConfig))]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    public sealed partial class ProgramConfigContext : JsonSerializerContext { }

    public static class BotContainer
    {
        public static void RunBots(ProgramConfig prog)
        {
            IPokeBotRunner env = GetRunner(prog);
            foreach (var bot in prog.Bots)
            {
                bot.Initialize();
                if (!AddBot(env, bot, prog.Mode))
                    Console.WriteLine($"Failed to add bot: {bot}");
            }

            LogUtil.Forwarders.Add((msg, ident) => Console.WriteLine($"{ident}: {msg}"));
            env.StartAll();
            Console.WriteLine($"Started all bots (Count: {prog.Bots.Length}.");
            Console.WriteLine("Press any key to stop execution and quit. Feel free to minimize this window!");
            Console.ReadKey();
            env.StopAll();
        }

        private static IPokeBotRunner GetRunner(ProgramConfig prog) => prog.Mode switch
        {
            ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(prog.Hub, new BotFactory8()),
            ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(prog.Hub, new BotFactory8BS()),
            ProgramMode.LA => new PokeBotRunnerImpl<PA8>(prog.Hub, new BotFactory8LA()),
            ProgramMode.SV => new PokeBotRunnerImpl<PK9>(prog.Hub, new BotFactory9SV()),
            _ => throw new IndexOutOfRangeException("Unsupported mode."),
        };

        private static bool AddBot(IPokeBotRunner env, PokeBotState cfg, ProgramMode mode)
        {
            if (!cfg.IsValid())
            {
                Console.WriteLine($"{cfg}'s config is not valid.");
                return false;
            }

            PokeRoutineExecutorBase newBot;
            try
            {
                newBot = env.CreateBotFromConfig(cfg);
            }
            catch
            {
                Console.WriteLine($"Current Mode ({mode}) does not support this type of bot ({cfg.CurrentRoutineType}).");
                return false;
            }
            try
            {
                env.Add(newBot);
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
