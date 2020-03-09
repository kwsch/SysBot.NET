using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public abstract class BotEnvironment
    {
        protected readonly PokeTradeHub<PK8> Hub;
        private readonly CancellationTokenSource Source = new CancellationTokenSource();
        public readonly List<PokeRoutineExecutor> Bots = new List<PokeRoutineExecutor>();
        public bool CanStart => Hub.Bots.Count != 0;
        public bool CanStop => IsRunning;
        public bool IsRunning { get; private set; }

        protected BotEnvironment(PokeTradeHub<PK8> hub) => Hub = hub;
        protected BotEnvironment(PokeTradeHubConfig config) => Hub = new PokeTradeHub<PK8>(config);

        public void Start(BotList cfg)
        {
            Hub.Bots.Clear();
            Hub.Counts.ReloadCounts(); // if user modified them

            CreateBots(cfg.Bots);

            var token = Source.Token;
            var tasks = CreateBotTasks(token);
            Task.Run(() => Task.WhenAll(tasks), token);
            IsRunning = true;
        }

        private List<Task> CreateBotTasks(CancellationToken token)
        {
            var tasks = new List<Task>();
            AddIntegrations();

            tasks.AddRange(Bots.Select(b => b.RunAsync(token)));
            bool hasTradeBot = Bots.Any(z => z is PokeTradeBot);
            if (hasTradeBot)
                AddTradeBotMonitors(tasks, token);
            return tasks;
        }

        protected virtual void AddIntegrations() { }

        private void AddTradeBotMonitors(List<Task> tasks, CancellationToken token)
        {
            var path = Hub.Config.Folder.DistributeFolder;
            if (!Directory.Exists(path))
                LogUtil.LogError("The distribution folder was not found. Please verify that it exists!", "Hub");

            var pool = Hub.Ledy.Pool;
            if (!pool.Reload())
                LogUtil.LogError("Nothing to distribute for Empty Trade Queues!", "Hub");

            tasks.Add(MonitorOpenQueue(token));
        }

        private async Task MonitorOpenQueue(CancellationToken token)
        {
            var queues = Hub.Queues.Info;
            var settings = Hub.Config.Queues;
            float secWaited = 0;

            const int sleepDelay = 0_500;
            const float sleepSeconds = sleepDelay / 1000f;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(sleepDelay, token).ConfigureAwait(false);
                var mode = settings.QueueToggleMode;
                if (!UpdateCanQueue(mode, settings, queues, secWaited))
                {
                    secWaited += sleepSeconds;
                    continue;
                }

                secWaited = 0;
                var state = queues.GetCanQueue()
                    ? "Users are now able to join the trade queue."
                    : "Changed queue settings: **Users CANNOT join the queue until it is turned back on.**";
                EchoUtil.Echo(state);
            }
        }

        private static bool UpdateCanQueue(QueueOpening mode, QueueSettings settings, TradeQueueInfo<PK8> queues, float secWaited)
        {
            return mode switch
            {
                QueueOpening.Threshold => CheckThreshold(settings, queues),
                QueueOpening.Interval => CheckInterval(settings, queues, secWaited),
                _ => false
            };
        }

        private static bool CheckInterval(QueueSettings settings, TradeQueueInfo<PK8> queues, float secWaited)
        {
            if (settings.CanQueue)
            {
                if (secWaited >= settings.IntervalOpenFor)
                    queues.ToggleQueue();
                else
                    return false;
            }
            else
            {
                if (secWaited >= settings.IntervalCloseFor)
                    queues.ToggleQueue();
                else
                    return false;
            }

            return true;
        }

        private static bool CheckThreshold(QueueSettings settings, TradeQueueInfo<PK8> queues)
        {
            if (settings.CanQueue)
            {
                if (queues.Count >= settings.ThresholdLock)
                    queues.ToggleQueue();
                else
                    return false;
            }
            else
            {
                if (queues.Count <= settings.ThresholdUnlock)
                    queues.ToggleQueue();
                else
                    return false;
            }

            return true;
        }

        private void CreateBots(IEnumerable<PokeBotConfig> bots)
        {
#if DEBUG
            if (Hub.Config.SkipConsoleBotCreation)
                return;
#endif

            foreach (var cfg in bots)
            {
                var bot = GetBotFromConfig(cfg);
                Bots.Add(bot);
            }
        }

        private PokeRoutineExecutor GetBotFromConfig(PokeBotConfig cfg)
        {
            switch (cfg.NextRoutineType)
            {
                case PokeRoutineType.Idle:
                case PokeRoutineType.SurpriseTrade:
                case PokeRoutineType.FlexTrade:
                case PokeRoutineType.LinkTrade:
                case PokeRoutineType.Clone:
                case PokeRoutineType.Dump:
                case PokeRoutineType.SeedCheck:
                    return new PokeTradeBot(Hub, cfg);

                case PokeRoutineType.EggFetch:
                    return new EggBot(cfg, Hub.Config.Egg, Hub.Config.Folder, Hub.Counts);

                case PokeRoutineType.FossilBot:
                    return new FossilBot(cfg, Hub.Config.Fossil, Hub.Config.Folder, Hub.Counts);

                case PokeRoutineType.RaidBot:
                    return new RaidBot(cfg, Hub.Config.Raid, Hub.Config.Folder, Hub.Counts);

                default:
                    throw new ArgumentException(nameof(cfg.NextRoutineType));
            }
        }

        public void Stop()
        {
            Source.Cancel();
            IsRunning = false;

            // bots currently don't de-register
            Thread.Sleep(100);
            int count = Hub.BotSync.Barrier.ParticipantCount;
            if (count != 0)
                Hub.BotSync.Barrier.RemoveParticipants(count);

            DetatchBots();
        }

        private void DetatchBots()
        {
            foreach (var bot in Bots)
            {
                Task.Run(() => bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None));
            }
        }

        public void Pause()
        {
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Config.Pause();
            IsRunning = false;
        }

        public void Resume()
        {
            IsRunning = true;
            // Tell all the bots to go to Idle after finishing.
            foreach (var b in Bots)
                b.Config.Resume();
        }
    }
}