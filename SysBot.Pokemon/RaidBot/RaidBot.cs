using PKHeX.Core;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class RaidBot : PokeRoutineExecutor
    {
        private readonly BotCompleteCounts Counts;
        private readonly RaidSettings Settings;
        public readonly IDumper Dump;
        private readonly bool ldn;

        public RaidBot(PokeBotConfig cfg, RaidSettings raid, IDumper folder, BotCompleteCounts counts) : base(cfg)
        {
            Settings = raid;
            Dump = folder;
            Counts = counts;
            ldn = Settings.UseLdnMitm;
        }

        private int encounterCount;

        protected override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main RaidBot loop.");
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.RaidBot)
            {
                int code = Settings.RaidCode;
                bool airplane = await HostRaidAsync(sav, code, token).ConfigureAwait(false);
                await ResetGameAsync(airplane, token).ConfigureAwait(false);

                encounterCount++;
                Log($"Raid host {encounterCount} finished.");
                Counts.AddCompletedRaids();
            }
        }

        private async Task<bool> HostRaidAsync(SAV8SWSH sav, int code, CancellationToken token)
        {
            // Connect to Y-Comm
            await EnsureConnectedToYComm(token).ConfigureAwait(false);

            // Press A and stall out a bit for the loading
            await Click(A, 5000, token).ConfigureAwait(false);

            if (code >= 0)
            {
                // Set Link code
                await Click(PLUS, 1000, token).ConfigureAwait(false);
                await EnterTradeCode(code, token).ConfigureAwait(false);

                // Raid barrier here maybe?
                await Click(PLUS, 2_000, token).ConfigureAwait(false);
                await Click(A, 1000, token).ConfigureAwait(false);
            }

            // Invite others, confirm Pokémon and wait
            await Click(A, 5000, token).ConfigureAwait(false);
            await Click(DUP, 1000, token).ConfigureAwait(false);
            await Click(A, 1000, token).ConfigureAwait(false);

            // Use Offset to actually calculate this value and press A
            var timetowait = 3 * 60 * 1000;
            while (timetowait > 0)
            {
                bool result = await CheckIfQueueFullAsync().ConfigureAwait(false);
                if (result)
                    break;

                await Task.Delay(1000, token).ConfigureAwait(false);
                timetowait -= 1000;
            }

            if (timetowait > 0)
            {
                Log("All participants have joined.");
                await Click(A, 3000, token).ConfigureAwait(false);
            }
            else
            {
                Log("Not all participants have joined. Continuing anyway!");
                await Click(A, 3000, token).ConfigureAwait(false);
            }

            Log($"Hosting raid as {sav.OT} with code: {code:0000}.");
            await Task.Delay(10_000, token).ConfigureAwait(false);

            return false;
        }

        // Check if queue is full and set true if it is
        private async Task<bool> CheckIfQueueFullAsync()
        {
            await Task.Delay(1).ConfigureAwait(false);
            return false;
        }

        private async Task ResetGameAsync(bool airplane, CancellationToken token)
        {
            if (!ldn)
                await ResetRaidCloseGame(token).ConfigureAwait(false);
            else if (airplane)
                await ResetRaidAirplaneLDN(token).ConfigureAwait(false);
            else
                await ResetRaidCloseGameLDN(token).ConfigureAwait(false);
        }

        private async Task ResetRaidCloseGameLDN(CancellationToken token)
        {
            Log("Resetting raid by restarting the game");
            // Close out of the game
            await Click(HOME, 3_000, token).ConfigureAwait(false);
            await Click(X, 1_000, token).ConfigureAwait(false);
            await Click(A, 5_000, token).ConfigureAwait(false); // Closing software prompt
            Log("Closed out of the game!");

            // Open game and select profile
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            Log("Restarting the game!");

            // Switch Logo lag, skip cutscene, game load screen
            await Task.Delay(25_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Task.Delay(10_000, token).ConfigureAwait(false);

            while (!await IsCorrectScreen(PokeDataOffsets.CurrentScreen_WildArea, token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }

            Log("Back in the overworld!");

            // Reconnect to ycomm.
            await EnsureConnectedToYComm(token).ConfigureAwait(false);
            Log("Reconnected to Y-Comm!");
        }

        private async Task ResetRaidAirplaneLDN(CancellationToken token)
        {
            Log("Resetting raid using Airplane Mode method");
            // Airplane mode method (only works when you connect with someone but faster)
            // Need to test if ldn_mitm crashes
            // Side menu
            await PressAndHold(HOME, 2_000, 1_000, token).ConfigureAwait(false);

            // Navigate to Airplane mode
            for (int i = 0; i < 4; i++)
                await Click(DDOWN, 1_200, token).ConfigureAwait(false);

            // Press A to turn on Airplane mode and then A again to turn it off (causes a freeze for a solid minute?)
            await Click(A, 1_200, token).ConfigureAwait(false);
            await Click(A, 1_200, token).ConfigureAwait(false);
            await Click(B, 1_200, token).ConfigureAwait(false);
            Log("Toggled Airplane Mode!");

            // Press OK on the error
            await Click(A, 1_200, token).ConfigureAwait(false);

            while (!await IsCorrectScreen(PokeDataOffsets.CurrentScreen_WildArea, token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
            Log("Back in the overworld!");

            // Reconnect to ycomm.
            await EnsureConnectedToYComm(token).ConfigureAwait(false);
            Log("Reconnected to Y-Comm!");
        }

        private async Task ResetRaidCloseGame(CancellationToken token)
        {
            var buttons = new[]
            {
                HOME, X, A, // Close out of the game
                A, A, // Open game and select profile
                B, B, B, B, B,// Delay 20 seconds for switch logo lag
                A, B, B, // Overworld!
                Y, PLUS, // Connect to Y-Comm
                B, B, B, B // Ensure Overworld
            };
            await DaisyChainCommands(5_000, buttons, token).ConfigureAwait(false);
        }
    }
}