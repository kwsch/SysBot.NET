using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Z3;
using System;
using System.IO;
using System.Text.Json;

namespace SysBot.Pokemon.ConsoleApp;

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
            PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>();
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
        var cfg = new ProgramConfig { Bots = [bot] };
        var created = JsonSerializer.Serialize(cfg, ProgramConfigContext.Default.ProgramConfig);
        File.WriteAllText(ConfigPath, created);
        Console.WriteLine("Created new config file since none was found in the program's path. Please configure it and restart the program.");
        Console.WriteLine("It is suggested to configure this config file using the GUI project if possible, as it will help you assign values correctly.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}

public static class BotContainer
{
    private static IPokeBotRunner? Environment;
    private static bool IsRunning => Environment != null;
    private static bool IsStopping;

    public static void RunBots(ProgramConfig prog)
    {
        IPokeBotRunner env = GetRunner(prog);
        foreach (var bot in prog.Bots)
        {
            bot.Initialize();
            if (!AddBot(env, bot, prog.Mode))
                Console.WriteLine($"Failed to add bot: {bot}");
        }

        LogUtil.Forwarders.Add(ConsoleForwarder.Instance);
        env.StartAll();
        Console.WriteLine($"Started all bots (Count: {prog.Bots.Length}).");

        Environment = env;
        WaitForExit();
    }

    private static void WaitForExit()
    {
        var msg = Console.IsInputRedirected
            ? "Running without console input. Waiting for exit signal."
            : "Press CTRL-C to stop execution. Feel free to minimize this window.";
        Console.WriteLine(msg);

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            if (IsStopping)
                return; // Already stopping, don't double stop.
            // Try as best we can to shut down.
            StopProcess("Process exit detected. Stopping all bots.");
        };
        Console.CancelKeyPress += (_, e) =>
        {
            if (IsStopping)
                return; // Already stopping, don't double stop.
            e.Cancel = true; // Gracefully exit after stopping all bots.
            StopProcess("Cancel key detected. Stopping all bots.");
        };

        while (IsRunning)
            System.Threading.Thread.Sleep(1000);
    }

    private static void StopProcess(string message)
    {
        IsStopping = true;
        Console.WriteLine(message);
        Environment?.StopAll();
        Environment = null;
    }

    private static IPokeBotRunner GetRunner(ProgramConfig prog) => prog.Mode switch
    {
        ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(prog.Hub, new BotFactory8SWSH()),
        ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(prog.Hub, new BotFactory8BS()),
        ProgramMode.LA   => new PokeBotRunnerImpl<PA8>(prog.Hub, new BotFactory8LA()),
        ProgramMode.SV   => new PokeBotRunnerImpl<PK9>(prog.Hub, new BotFactory9SV()),
        ProgramMode.LZA  => new PokeBotRunnerImpl<PA9>(prog.Hub, new BotFactory9LZA()),
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
