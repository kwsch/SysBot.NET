using PKHeX.Core;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon.BotTournament
{
    public class TournamentBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly BotCompleteCounts Counts;
        private readonly IDumper DumpSetting;

        public TournamentBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Counts = Hub.Counts;
            DumpSetting = Hub.Config.Folder;
        }

        public override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.TournamentBot)
            {
                Config.IterateNextRoutine();
                await GetOutOfCurrentWindowInOverworld(token).ConfigureAwait(false);

                await GoToSharingLocalTournament(token).ConfigureAwait(false);

                await SwitchToLanPlayMode(token).ConfigureAwait(false);

                await Task.Delay(1_000, token).ConfigureAwait(false);

                await Click(A, 1_000, token).ConfigureAwait(false);

                while (Hub.Config.Tournament.ContinueAfterSending && !token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.TournamentBot)
                {
                    await Click(A, 1_000, token).ConfigureAwait(false);
                }
            }
        }

        private async Task GoToSharingLocalTournament(CancellationToken token)
        {
            await Click(X, 1_000, token).ConfigureAwait(false);
            await Click(DDOWN, 500, token).ConfigureAwait(false);
            await Click(DRIGHT, 500, token).ConfigureAwait(false);
            await Click(DRIGHT, 500, token).ConfigureAwait(false);
            await Click(DRIGHT, 500, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);
            await Click(DRIGHT, 500, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);
            await Click(DDOWN, 500, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
        }

        private async Task SwitchToLanPlayMode(CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.Hold(R, UseCRLF), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Hold(L, UseCRLF), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Hold(DUP, UseCRLF), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Hold(LSTICK, UseCRLF), token).ConfigureAwait(false);

            await Task.Delay(5_000, token).ConfigureAwait(false);

            await Connection.SendAsync(SwitchCommand.Release(R, UseCRLF), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Release(L, UseCRLF), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Release(DUP, UseCRLF), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Release(LSTICK, UseCRLF), token).ConfigureAwait(false);
        }

        private async Task GetOutOfCurrentWindowInOverworld(CancellationToken token)
        {
            await Click(B, 500, token).ConfigureAwait(false);
            await Click(B, 500, token).ConfigureAwait(false);
            await Click(B, 500, token).ConfigureAwait(false);
            await Click(A, 500, token).ConfigureAwait(false);
            await Click(B, 500, token).ConfigureAwait(false);
            await Click(B, 500, token).ConfigureAwait(false);
            await Click(B, 500, token).ConfigureAwait(false);
        }
    }
}
