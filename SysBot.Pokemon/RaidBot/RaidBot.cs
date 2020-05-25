using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class RaidBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly BotCompleteCounts Counts;
        private readonly RaidSettings Settings;
        public readonly IDumper Dump;
        private readonly bool ldn;

        public RaidBot(PokeBotConfig cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Settings = hub.Config.Raid;
            Dump = hub.Config.Folder;
            Counts = hub.Counts;
            ldn = Settings.UseLdnMitm;
        }

        private int encounterCount;

        protected override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);

            var originalTextSpeed = await EnsureTextSpeedFast(token).ConfigureAwait(false);

            Log("Starting main RaidBot loop.");

            if (Hub.Config.Raid.MinTimeToWait < 0 || Hub.Config.Raid.MinTimeToWait > 180)
            {
                Log("Time to wait must be between 0 and 180 seconds.");
                return;
            }

            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.RaidBot)
            {
                Config.IterateNextRoutine();
                int code = Settings.GetRandomRaidCode();
                bool airplane = await HostRaidAsync(sav, code, token).ConfigureAwait(false);

                encounterCount++;
                Log($"Raid host {encounterCount} finished.");
                Counts.AddCompletedRaids();

                await ResetGameAsync(airplane, token).ConfigureAwait(false);
            }
            await SetTextSpeed(originalTextSpeed, token).ConfigureAwait(false);
        }

        private async Task<bool> HostRaidAsync(SAV8SWSH sav, int code, CancellationToken token)
        {
            // Connect to Y-Comm
            await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);

            // Press A and stall out a bit for the loading
            await Click(A, 5_000 + Hub.Config.Raid.ExtraTimeLoadRaid, token).ConfigureAwait(false);

            var msg = code < 0 ? "no Link Code" : $"code: {code:0000}";
            EchoUtil.Echo($"Raid lobby is open with {msg}.");

            if (code >= 0)
            {
                // Set Link code
                await Click(PLUS, 1_000, token).ConfigureAwait(false);
                await EnterTradeCode(code, token).ConfigureAwait(false);

                // Raid barrier here maybe?
                await Click(PLUS, 2_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }

            // Invite others, confirm Pokémon and wait
            await Click(A, 7_000 + Hub.Config.Raid.ExtraTimeOpenRaid, token).ConfigureAwait(false);
            await Click(DUP, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);

            var timetowait = Hub.Config.Raid.MinTimeToWait * 1_000;
            var timetojoinraid = 175_000 - timetowait;

            Log("Waiting on raid party...");
            // Wait the minimum timer or until raid party fills up.
            while (timetowait > 0 && !await GetRaidPartyIsFullAsync(token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timetowait -= 1_000;
            }

            EchoUtil.Echo($"Raid will be starting soon with {msg}.");

            // Wait a few seconds for people to lock in.
            await Task.Delay(5_000, token).ConfigureAwait(false);

            /* Press A and check if we entered a raid.  If other users don't lock in,
               it will automatically start once the timer runs out. If we don't make it into
               a raid by the end, something has gone wrong and we should quit trying. */
            while (timetojoinraid > 0 && !await IsInBattle(token).ConfigureAwait(false))
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
                timetojoinraid -= 0_500;
            }

            Log("Finishing raid routine.");
            await Task.Delay(5_000 + Hub.Config.Raid.ExtraTimeEndRaid, token).ConfigureAwait(false);

            return false;
        }

        private async Task<bool> GetRaidPartyIsFullAsync(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(RaidTrainerFullOffset, 4, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0) == 0xFFFFFFFF;
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
            await Click(HOME, 4_000, token).ConfigureAwait(false);
            await Click(X, 1_000, token).ConfigureAwait(false);
            await Click(A, 5_000, token).ConfigureAwait(false); // Closing software prompt
            Log("Closed out of the game!");

            // Open game and select profile
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            Log("Restarting the game!");

            // Switch Logo lag, skip cutscene, game load screen
            await Task.Delay(25_000, token).ConfigureAwait(false);

            while (!await IsCorrectScreen(CurrentScreen_WildArea, token).ConfigureAwait(false))
                await Click(A, 1_000, token).ConfigureAwait(false);

            Log("Back in the overworld!");

            // Reconnect to Y-Comm.
            await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);
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

            // Reconnect to Y-Comm.
            await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);
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
