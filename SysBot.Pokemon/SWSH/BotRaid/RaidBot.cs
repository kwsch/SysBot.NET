using PKHeX.Core;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class RaidBot : PokeRoutineExecutor8, ICountBot
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly RaidSettings Settings;
        public ICountSettings Counts => Settings;

        public RaidBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Settings = hub.Config.RaidSWSH;
        }

        // Cached offsets that stay the same per session.
        private ulong OverworldOffset;

        private int encounterCount;
        private bool deleteFriends;
        private bool addFriends;
        private readonly bool[] PlayerReady = new bool[4];
        private int raidBossSpecies = -1;

        public override async Task MainLoop(CancellationToken token)
        {
            if (Settings.TimeToWait is < 0 or > 180)
            {
                Log("Time to wait must be between 0 and 180 seconds.");
                return;
            }

            try
            {
                Log("Identifying trainer data of the host console.");
                await IdentifyTrainer(token).ConfigureAwait(false);
                await InitializeSessionOffsets(token).ConfigureAwait(false);

                await InitializeHardware(Settings, token).ConfigureAwait(false);

                Log("Starting main RaidBot loop.");
                await InnerLoop(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }

            Log($"Ending {nameof(RaidBot)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        private async Task InnerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.RaidBot)
            {
                Config.IterateNextRoutine();
                addFriends = false;
                deleteFriends = false;

                // If they set this to 0, they want to add and remove friends before hosting any raids.
                if (Settings.InitialRaidsToHost == 0 && encounterCount == 0)
                {
                    if (Settings.NumberFriendsToAdd > 0)
                        addFriends = true;
                    if (Settings.NumberFriendsToDelete > 0)
                        deleteFriends = true;

                    if (addFriends || deleteFriends)
                    {
                        // Back out of the game.
                        await Click(B, 0_500, token).ConfigureAwait(false);
                        await Click(HOME, 4_000, token).ConfigureAwait(false);
                        await DeleteAddFriends(token).ConfigureAwait(false);
                        await Click(HOME, 1_000, token).ConfigureAwait(false);
                    }
                }

                encounterCount++;

                // Check if we're scheduled to delete or add friends after this raid is hosted.
                // If we're changing friends, we'll echo while waiting on the lobby to fill up.
                if (Settings.InitialRaidsToHost <= encounterCount)
                {
                    if (Settings.NumberFriendsToAdd > 0 && Settings.RaidsBetweenAddFriends > 0)
                        addFriends = (encounterCount - Settings.InitialRaidsToHost) % Settings.RaidsBetweenAddFriends == 0;
                    if (Settings.NumberFriendsToDelete > 0 && Settings.RaidsBetweenDeleteFriends > 0)
                        deleteFriends = (encounterCount - Settings.InitialRaidsToHost) % Settings.RaidsBetweenDeleteFriends == 0;
                }

                int code = Settings.GetRandomRaidCode();
                await HostRaidAsync(code, token).ConfigureAwait(false);

                Log($"Raid host {encounterCount} finished.");
                Settings.AddCompletedRaids();

                await ResetGameAsync(token).ConfigureAwait(false);
            }
        }

        public override async Task HardStop()
        {
            await CleanExit(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task HostRaidAsync(int code, CancellationToken token)
        {
            // Connect to Y-Comm
            await EnsureConnectedToYComm(OverworldOffset, Hub.Config, token).ConfigureAwait(false);

            // Press A and stall out a bit for the loading
            await Click(A, 5_000 + Hub.Config.Timings.ExtraTimeLoadRaid, token).ConfigureAwait(false);

            if (raidBossSpecies == -1)
            {
                var data = await Connection.ReadBytesAsync(RaidBossOffset, 2, token).ConfigureAwait(false);
                raidBossSpecies = BitConverter.ToUInt16(data, 0);
            }
            Log($"Initializing raid for {(Species)raidBossSpecies}.");

            if (code >= 0)
            {
                // Set Link code
                await Click(PLUS, 1_000, token).ConfigureAwait(false);
                await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);
                await Click(PLUS, 4_000, token).ConfigureAwait(false);
                Log("Confirming the Link Code.");
                await Click(A, 2_000, token).ConfigureAwait(false);
            }

            if (addFriends && !string.IsNullOrEmpty(Settings.FriendCode))
                EchoUtil.Echo($"Send a friend request to Friend Code **{Settings.FriendCode}** to join in! Friends will be added after this raid.");

            // Invite others, confirm Pokémon and wait
            Log("Inviting others to the raid...");
            await Click(A, 7_000 + Hub.Config.Timings.ExtraTimeOpenRaid, token).ConfigureAwait(false);
            await Click(DUP, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);

            var linkcodemsg = code < 0 ? "no Link Code" : $"code **{code:0000 0000}**";

            string raiddescmsg = string.IsNullOrEmpty(Settings.RaidDescription) ? ((Species)raidBossSpecies).ToString() : Settings.RaidDescription;
            EchoUtil.Echo($"Raid lobby for {raiddescmsg} is open with {linkcodemsg}.");

            var timetowait = Settings.TimeToWait * 1_000;
            var timetojoinraid = 175_000 - timetowait;

            Log("Waiting on raid party...");
            // Wait the minimum timer or until raid party fills up.
            while (timetowait > 0 && !await GetRaidPartyReady(token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timetowait -= 1_000;
            }

            await Task.Delay(1_000, token).ConfigureAwait(false);
            EchoUtil.Echo($"Raid is starting now with {linkcodemsg}.");

            /* Press A and check if we entered a raid.  If other users don't lock in,
               it will automatically start once the timer runs out. If we don't make it into
               a raid by the end, something has gone wrong and we should quit trying. */
            while (timetojoinraid > 0 && !await IsInBattle(token).ConfigureAwait(false))
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
                timetojoinraid -= 0_500;
            }

            for (int i = 0; i < 4; i++)
                PlayerReady[i] = false;

            Log("Finishing raid routine.");
            await Task.Delay(1_000 + Hub.Config.Timings.ExtraTimeEndRaid, token).ConfigureAwait(false);
        }

        private async Task<bool> GetRaidPartyReady(CancellationToken token)
        {
            bool ready = true;
            for (uint i = 0; i < 4; i++)
            {
                if (!await ConfirmPlayerReady(i, token).ConfigureAwait(false))
                    ready = false;
            }
            return ready;
        }

        private async Task<bool> ConfirmPlayerReady(uint player, CancellationToken token)
        {
            if (PlayerReady[player])
                return true;

            var ofs = RaidP0PokemonOffset + (0x30 * player);

            // Check if the player has locked in.
            var data = await Connection.ReadBytesAsync(ofs + RaidLockedInIncr, 1, token).ConfigureAwait(false);
            if (data[0] == 0)
                return false;

            PlayerReady[player] = true;

            // If we get to here, they're locked in and should have a Pokémon selected.
            if (Settings.EchoPartyReady)
            {
                data = await Connection.ReadBytesAsync(ofs, 2, token).ConfigureAwait(false);
                var dexno = BitConverter.ToUInt16(data, 0);

                data = await Connection.ReadBytesAsync(ofs + RaidAltFormInc, 1, token).ConfigureAwait(false);
                var altformstr = data[0] == 0 ? "" : "-" + data[0];

                data = await Connection.ReadBytesAsync(ofs + RaidShinyIncr, 1, token).ConfigureAwait(false);
                var shiny = data[0] == 1 ? "★ " : "";

                data = await Connection.ReadBytesAsync(ofs + RaidGenderIncr, 1, token).ConfigureAwait(false);
                var gender = data[0] == 0 ? " (M)" : (data[0] == 1 ? " (F)" : "");

                EchoUtil.Echo($"Player {player + 1} is ready with {shiny}{(Species)dexno}{altformstr}{gender}!");
            }

            return true;
        }

        private async Task ResetGameAsync(CancellationToken token)
        {
            Log("Resetting raid by restarting the game");
            await CloseGame(Hub.Config, token).ConfigureAwait(false);

            if (addFriends || deleteFriends)
                await DeleteAddFriends(token).ConfigureAwait(false);

            await StartGame(Hub.Config, token).ConfigureAwait(false);
            await InitializeSessionOffsets(token).ConfigureAwait(false);

            await EnsureConnectedToYComm(OverworldOffset, Hub.Config, token).ConfigureAwait(false);
            Log("Reconnected to Y-Comm!");
        }

        // For pointer offsets that don't change per session are accessed frequently, so set these each time we start.
        private async Task InitializeSessionOffsets(CancellationToken token)
        {
            Log("Caching session offsets...");
            OverworldOffset = await SwitchConnection.PointerAll(Offsets.OverworldPointer, token).ConfigureAwait(false);
        }

        private async Task DeleteAddFriends(CancellationToken token)
        {
            await NavigateToProfile(token).ConfigureAwait(false);

            // Delete before adding to avoid deleting new friends.
            if (deleteFriends)
            {
                Log("Deleting friends.");
                await NavigateFriendsMenu(true, token).ConfigureAwait(false);
                for (int i = 0; i < Settings.NumberFriendsToDelete; i++)
                    await DeleteFriend(token).ConfigureAwait(false);
            }

            // If we're deleting friends and need to add friends, it's cleaner to back out 
            // to Home and re-open the profile in case we ran out of friends to delete.
            if (deleteFriends && addFriends)
            {
                Log("Navigating back to add friends.");
                await Click(HOME, 2_000, token).ConfigureAwait(false);
                await NavigateToProfile(token).ConfigureAwait(false);
            }

            if (addFriends)
            {
                Log("Adding friends.");
                await NavigateFriendsMenu(false, token).ConfigureAwait(false);
                for (int i = 0; i < Settings.NumberFriendsToAdd; i++)
                    await AddFriend(token).ConfigureAwait(false);
            }

            addFriends = false;
            deleteFriends = false;
            await Click(HOME, 2_000, token).ConfigureAwait(false);
        }

        // Goes from Home screen hovering over the game to the correct profile
        private async Task NavigateToProfile(CancellationToken token)
        {
            int delay = Hub.Config.Timings.KeypressTime;

            await Click(DUP, delay, token).ConfigureAwait(false);
            for (int i = 1; i < Settings.ProfileNumber; i++)
                await Click(DRIGHT, delay, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);
        }

        // Gets us on the friend card if it exists after HOME button has been pressed.
        // Should already be on either "Friend List" or "Add Friend"
        private async Task NavigateFriendsMenu(bool delete, CancellationToken token)
        {
            int delay = Hub.Config.Timings.KeypressTime;

            // Go all the way up, then down 1. Reverse for adding friends.
            if (delete)
            {
                for (int i = 0; i < 5; i++)
                    await Click(DUP, delay, token).ConfigureAwait(false);
                await Click(DDOWN, 1_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);

                await NavigateFriends(Settings.RowStartDeletingFriends, 4, token).ConfigureAwait(false);
            }
            else
            {
                for (int i = 0; i < 5; i++)
                    await Click(DDOWN, delay, token).ConfigureAwait(false);
                await Click(DUP, 1_000, token).ConfigureAwait(false);

                // Click into the menu.
                await Click(A, 1_000, token).ConfigureAwait(false);
                await Click(A, 2_500, token).ConfigureAwait(false);

                await NavigateFriends(Settings.RowStartAddingFriends, 5, token).ConfigureAwait(false);
            }
        }

        // Navigates to the specified row and column.
        private async Task NavigateFriends(int row, int column, CancellationToken token)
        {
            int delay = Hub.Config.Timings.KeypressTime;

            if (row == 1)
                return;

            for (int i = 1; i < row; i++)
                await Click(DDOWN, delay, token).ConfigureAwait(false);

            for (int i = 1; i < column; i++)
                await Click(DRIGHT, delay, token).ConfigureAwait(false);
        }

        // Deletes one friend. Should already be hovering over the friend card.
        private async Task DeleteFriend(CancellationToken token)
        {
            await Click(A, 1_500, token).ConfigureAwait(false);
            // Opens Options
            await Click(DDOWN, 0_600, token).ConfigureAwait(false);
            await Click(A, 0_600, token).ConfigureAwait(false);
            // Click "Remove Friend", confirm "Delete", return to next card.
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 5_000 + Hub.Config.Timings.ExtraTimeDeleteFriend, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
        }

        // Adds one friend. Timing may need to be adjusted since delays vary with connection.
        private async Task AddFriend(CancellationToken token)
        {
            await Click(A, 3_500 + Hub.Config.Timings.ExtraTimeAddFriend, token).ConfigureAwait(false);
            await Click(A, 3_000 + Hub.Config.Timings.ExtraTimeAddFriend, token).ConfigureAwait(false);
        }
    }
}
