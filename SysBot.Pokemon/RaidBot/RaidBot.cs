using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class RaidBot : PokeRoutineExecutor
    {
        private readonly BotCompleteCounts Counts;
        public readonly IDumper DumpSetting;

        public RaidBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Counts = hub.Counts;
            DumpSetting = hub.Config;
        }

        private int encounterCount;

        protected override async Task MainLoop(CancellationToken token)
        {
            await DaisyChainCommands(1000, new [] {Y, B, Y, B, Y, B}, token).ConfigureAwait(false);
        }
    }
}