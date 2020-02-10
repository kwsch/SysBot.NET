using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon.WinForms
{
    public sealed class BotEnvironment
    {
        private readonly PokeTradeHub<PK8> Hub = new PokeTradeHub<PK8>();
        private readonly CancellationTokenSource Source = new CancellationTokenSource();
        private readonly List<PokeRoutineExecutor> Bots = new List<PokeRoutineExecutor>();

        public bool CanStart => Hub.Bots.Count != 0;
        public bool CanStop => IsRunning;
        public bool IsRunning { get; private set; }

        public void Start(BotEnvironmentConfig cfg)
        {
            Hub.Config = cfg.Hub;
            Hub.CompletedTrades = cfg.Hub.CompletedTrades;
            CreateBots(cfg);

            var token = Source.Token;
            var tasks = CreateBotTasks(token);
            Task.Run(() => Task.WhenAll(tasks), token);
            IsRunning = true;
        }

        private List<Task> CreateBotTasks(CancellationToken token)
        {
            var tasks = Bots.Select(z => z.RunAsync(token)).ToList();
            bool tradeBot = Bots.Any(z => z is PokeTradeBot);
            if (tradeBot)
                AddTradeBotMonitors(tasks, token);
            return tasks;
        }

        private void AddTradeBotMonitors(ICollection<Task> tasks, CancellationToken token)
        {
            if (Hub.Config.DistributeWhileIdle)
            {
                var path = Hub.Config.DistributeFolder;
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(nameof(path));
                tasks.Add(Hub.MonitorTradeQueueAddIfEmpty(path, token));
            }
            if (Hub.Config.MonitorForPriorityTrades)
            {
                var path = Hub.Config.PriorityFolder;
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(nameof(path));
                tasks.Add(Hub.MonitorFolderAddPriority(path, PokeTradeHub<PK8>.LogNotifier, token));
            }
        }

        private void CreateBots(BotEnvironmentConfig cfg)
        {
            foreach (var c in cfg.Bots)
            {
                var bot = GetBotFromConfig(c);
                if (bot is IDumper d)
                {
                    d.Dump = Hub.Config.Dump;
                    d.DumpFolder = Hub.Config.DumpFolder;
                }
                Bots.Add(bot);
            }
        }

        private PokeRoutineExecutor GetBotFromConfig(PokeBotConfig cfg)
        {
            switch (cfg.NextRoutineType)
            {
                case PokeRoutineType.Idle:
                case PokeRoutineType.Reserved:
                case PokeRoutineType.LinkTrade:
                case PokeRoutineType.SurpriseTrade:
                    return new PokeTradeBot(Hub, cfg);

                case PokeRoutineType.EggFetch:
                    return new EggBot(cfg);

                default:
                    throw new ArgumentException(nameof(cfg.NextRoutineType));
            }
        }

        public void Stop()
        {
            Source.Cancel();
            IsRunning = false;
        }
    }
}