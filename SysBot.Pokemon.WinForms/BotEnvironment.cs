using System.Collections.Generic;
using System.Threading;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon;

namespace SysBot.WinForms
{
    public sealed class BotEnvironment
    {
        public readonly PokeTradeHub<PK8> Hub = new PokeTradeHub<PK8>();
        private CancellationTokenSource Source = new CancellationTokenSource();
        public List<SwitchBotConfig> Bots = new List<SwitchBotConfig>();

        public bool CanStart => Hub.Bots.Count != 0;
        public bool CanStop => IsRunning;
        public bool IsRunning { get; private set; }

        public void Start(BotEnvironmentConfig cfg)
        {
            Hub.Config = cfg.Hub;
            foreach (var bot in cfg.Bots)
            {

            }
            Source = new CancellationTokenSource();
            IsRunning = true;
        }

        public void Stop()
        {
            Source.Cancel();
            IsRunning = false;
        }
    }
}