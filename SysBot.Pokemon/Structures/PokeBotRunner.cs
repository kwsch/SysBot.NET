using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public abstract class PokeBotRunner : BotRunner<PokeBotConfig>
    {
        public readonly PokeTradeHub<PK8> Hub;

        protected PokeBotRunner(PokeTradeHub<PK8> hub) => Hub = hub;
        protected PokeBotRunner(PokeTradeHubConfig config) => Hub = new PokeTradeHub<PK8>(config);

        protected virtual void AddIntegrations() { }

        public override void Add(SwitchRoutineExecutor<PokeBotConfig> bot)
        {
            base.Add(bot);
            if (bot is PokeTradeBot b)
                Hub.Bots.Add(b);
        }

        public override bool Remove(string ip, bool callStop)
        {
            var bot = Bots.Find(z => z.Bot.Connection.IP == ip)?.Bot;
            if (bot is PokeTradeBot b)
                Hub.Bots.Remove(b);
            return base.Remove(ip, callStop);
        }

        public override void StartAll()
        {
            InitializeStart();

            if (!Hub.Config.SkipConsoleBotCreation)
                base.StartAll();
        }

        public override void InitializeStart()
        {
            Hub.Counts.LoadCountsFromConfig(); // if user modified them prior to start
            if (RunOnce)
                return;

            AutoLegalityWrapper.EnsureInitialized(Hub.Config.Legality);

            AddIntegrations();
            AddTradeBotMonitors();

            base.InitializeStart();
        }

        public override void StopAll()
        {
            base.StopAll();

            // bots currently don't de-register
            Thread.Sleep(100);
            int count = Hub.BotSync.Barrier.ParticipantCount;
            if (count != 0)
                Hub.BotSync.Barrier.RemoveParticipants(count);
        }

        public override void PauseAll()
        {
            if (!Hub.Config.SkipConsoleBotCreation)
                base.PauseAll();
        }

        public override void ResumeAll()
        {
            if (!Hub.Config.SkipConsoleBotCreation)
                base.ResumeAll();
        }

        private void AddTradeBotMonitors()
        {
            Task.Run(async () => await new QueueMonitor(Hub).MonitorOpenQueue(CancellationToken.None).ConfigureAwait(false));

            var path = Hub.Config.Folder.DistributeFolder;
            if (!Directory.Exists(path))
                LogUtil.LogError("The distribution folder was not found. Please verify that it exists!", "Hub");

            var pool = Hub.Ledy.Pool;
            if (!pool.Reload())
                LogUtil.LogError("Nothing to distribute for Empty Trade Queues!", "Hub");
        }

        public PokeRoutineExecutor CreateBotFromConfig(PokeBotConfig cfg)
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
                    return new EggBot(cfg, Hub);

                case PokeRoutineType.FossilBot:
                    return new FossilBot(cfg, Hub);

                case PokeRoutineType.RaidBot:
                    return new RaidBot(cfg, Hub);

                case PokeRoutineType.EncounterBot:
                    return new EncounterBot(cfg, Hub);

                case PokeRoutineType.RemoteControl:
                    return new RemoteControlBot(cfg);

                default:
                    throw new ArgumentException(nameof(cfg.NextRoutineType));
            }
        }
    }
}