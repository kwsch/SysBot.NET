using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using LogLevel = NLog.LogLevel;

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
            InitializeHubSettings(cfg.Hub);
            CreateBots(cfg.Bots);

            var token = Source.Token;
            var tasks = CreateBotTasks(token);
            Task.Run(() => Task.WhenAll(tasks), token);
            IsRunning = true;
        }

        private void InitializeHubSettings(PokeTradeHubConfig cfg)
        {
            Hub.Config = cfg;
            Hub.CompletedTrades = cfg.CompletedTrades;
            Hub.UseBarrier = cfg.SynchronizeLinkTradeBots;
        }

        private List<Task> CreateBotTasks(CancellationToken token)
        {
            var tasks = Bots.Select(b => b.RunAsync(token)).ToList();
            bool hasTradeBot = Bots.Any(z => z is PokeTradeBot);
            if (hasTradeBot)
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
                var task = Hub.MonitorTradeQueueAddIfEmpty(path, token);
                tasks.Add(task);

                if (Hub.Pool.Count == 0)
                    LogUtil.Log(LogLevel.Error, "Nothing to distribute for Empty Trade Queues!", "Hub");
            }
            if (Hub.Config.MonitorForPriorityTrades)
            {
                var path = Hub.Config.PriorityFolder;
                if (!Directory.Exists(path))
                    throw new DirectoryNotFoundException(nameof(path));
                var task = Hub.MonitorFolderAddPriority(path, PokeTradeHub<PK8>.LogNotifier, token);
                tasks.Add(task);
            }
        }

        private void CreateBots(IEnumerable<PokeBotConfig> bots)
        {
            foreach (var cfg in bots)
            {
                var bot = GetBotFromConfig(cfg);
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
                case PokeRoutineType.DuduBot:
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