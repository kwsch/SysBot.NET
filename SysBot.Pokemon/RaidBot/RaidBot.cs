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
        private bool ldn = true;

        public RaidBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Counts = hub.Counts;
            Settings = hub.Config;
            ldn = Settings.UseLdnMitm;
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
                await ResetGameAsync(ldn, token).ConfigureAwait(false);

                encounterCount++;
                Connection.Log($"Raid host {encounterCount} finished.");
                Counts.AddCompletedRaids();
            }
        }

        private async Task HostRaidAsync(SAV8SWSH sav, int code, CancellationToken token)
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

            // Invite others, confirm pokemon and wait
            await Click(A, 5000, token).ConfigureAwait(false);
            await Click(DUP, 1000, token).ConfigureAwait(false);
            await Click(A, 1000, token).ConfigureAwait(false);
            // Use Offset to actually calculate this value and press A
            var timetowait = 3 * 60 * 1000;
            var fullqueue = false;
            while (!fullqueue)
            {
                if (timetowait < 0)
                    break;
                await Task.Delay(1000, token).ConfigureAwait(false);
                timetowait -= 1000;
                // Check if queue is full and set true if it is
            }

            if (fullqueue)
                await Click(A, 3000, token).ConfigureAwait(false);

            Connection.Log($"Hosting raid as {sav.OT} with code: {code:0000}.");
            await Task.Delay(10000, token).ConfigureAwait(false);
        }

        private async Task ResetGameAsync(bool ldn, CancellationToken token)
        {
            if (!ldn)
            {
                var buttons = new[]
                {
                    HOME, X, A, // Close out of the game
                    A, A,       // Open game and select profile
                    B, B, B, B, // Delay 20 seconds for switch logo lag
                    A, B, B,    // Overworld!
                    Y, PLUS,    // Connect to Y-Comm
                    B, B, B, B  // Ensure Overworld
                };
                await DaisyChainCommands(5000, buttons, token).ConfigureAwait(false);
            }
            else
            {
                // Check if you are the only one in the raid. If not, set airplane to false.
                var airplane = false;

                if (airplane)
                {
                    Connection.Log("Resetting raid using Airplane Mode method");
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
                    Connection.Log("Toggled Airplane Mode!");

                    // Press OK on the error
                    await Click(A, 1_200, token).ConfigureAwait(false);

                    while (!await IsCorrectScreen(PokeDataOffsets.CurrentScreen_Overworld, token).ConfigureAwait(false))
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                    }

                    Connection.Log("Back in overworld, reconnecting to Y-Comm");
                    // Reconnect to ycomm.
                    await EnsureConnectedToYComm(token).ConfigureAwait(false);
                }

                else 
                {
                    Connection.Log("Resetting raid by restarting the game");
                    // Close out of the game
                    await Click(HOME, 3000, token).ConfigureAwait(false);
                    await Click(X, 1000, token).ConfigureAwait(false);
                    await Click(A, 5000, token).ConfigureAwait(false); // Closing software prompt
                    Connection.Log("Closed out of the game!");

                    // Open game and select profile
                    await Click(A, 1000, token).ConfigureAwait(false);
                    await Click(A, 1000, token).ConfigureAwait(false);
                    Connection.Log("Restarting the game!");

                    // Switch Logo lag, skip cutscene, game load screen
                    await Task.Delay(20000, token).ConfigureAwait(false);
                    await Click(A, 1000, token).ConfigureAwait(false);
                    await Task.Delay(10000, token).ConfigureAwait(false);

                    while (!await IsCorrectScreen(PokeDataOffsets.CurrentScreen_Overworld, token).ConfigureAwait(false))
                    {
                        await Task.Delay(1000, token).ConfigureAwait(false);
                    }
                    Connection.Log("Back in the overworld!");

                    // Reconnect to ycomm.
                    await EnsureConnectedToYComm(token).ConfigureAwait(false);
                    Connection.Log("Reconnected to Y-Comm!");
                }
            }
        }
    }
}