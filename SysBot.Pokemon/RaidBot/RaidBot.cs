using PKHeX.Core;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class RaidBot : PokeRoutineExecutor
    {
        private readonly BotCompleteCounts Counts;
        public readonly PokeTradeHubConfig Settings;

        public RaidBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Counts = hub.Counts;
            Settings = hub.Config;
        }

        private int encounterCount;

        protected override async Task MainLoop(CancellationToken token)
        {
            Connection.Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);

            Connection.Log("Starting main RaidBot loop.");
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.RaidBot)
            {
                int code = Settings.DistributionTradeCode;
                await HostRaidAsync(sav, code, token).ConfigureAwait(false);
                await ResetGameAsync(token).ConfigureAwait(false);

                encounterCount++;
                Connection.Log($"Raid host {encounterCount} finished.");
                Counts.AddCompletedRaids();
            }
        }

        private async Task HostRaidAsync(SAV8SWSH sav, int code, CancellationToken token)
        {
            Connection.Log($"Hosting raid as {sav.OT} with code: {code:0000}.");
            await Task.Delay(100, token).ConfigureAwait(false);
        }

        private async Task ResetGameAsync(CancellationToken token)
        {
            await DaisyChainCommands(1000, new[] { Y, B, Y, B, Y, B }, token).ConfigureAwait(false);
        }
    }
}