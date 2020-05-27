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

        public RaidBot(PokeBotConfig cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Settings = hub.Config.Raid;
            Counts = hub.Counts;
        }

        private int encounterCount;
        private bool deleteFriends = false;
        private bool addFriends = false;

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
                addFriends = false;
                deleteFriends = false;

                // If they set this to 0, they want to add and remove friends before hosting any raids.
                if (Settings.InitialRaidsToHost == 0 && encounterCount == 0)
                {
                    Log("Adding and removing friends.");
                    addFriends = true;
                    deleteFriends = true;

                    // Back out of the game.
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    await Click(HOME, 4_000, token).ConfigureAwait(false);
                    await DeleteAddFriends(token).ConfigureAwait(false);
                    await Click(HOME, 1_000, token).ConfigureAwait(false);
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
                await HostRaidAsync(sav, code, token).ConfigureAwait(false);

                Log($"Raid host {encounterCount} finished.");
                Counts.AddCompletedRaids();

                await ResetGameAsync(token).ConfigureAwait(false);
            }
            await SetTextSpeed(originalTextSpeed, token).ConfigureAwait(false);
        }

        private async Task<bool> HostRaidAsync(SAV8SWSH sav, int code, CancellationToken token)
        {
            // Connect to Y-Comm
            await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);

            // Press A and stall out a bit for the loading
            await Click(A, 5_000 + Hub.Config.Raid.ExtraTimeLoadRaid, token).ConfigureAwait(false);

            if (code >= 0)
            {
                // Set Link code
                await Click(PLUS, 1_000, token).ConfigureAwait(false);
                await EnterTradeCode(code, token).ConfigureAwait(false);
                await Click(PLUS, 2_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }

            // Invite others, confirm Pokémon and wait
            await Click(A, 7_000 + Hub.Config.Raid.ExtraTimeOpenRaid, token).ConfigureAwait(false);
            await Click(DUP, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);

            var msg = code < 0 ? "no Link Code" : $"code: {code:0000}";
            EchoUtil.Echo($"Raid lobby is open with {msg}.");

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

            if (addFriends && !string.IsNullOrEmpty(Settings.FriendCode))
                EchoUtil.Echo($"Send a friend request to Friend Code {Settings.FriendCode} to join in! Friends will be added after this raid.");

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

        private async Task ResetGameAsync(CancellationToken token)
        {
            Log("Resetting raid by restarting the game");
            // Close out of the game
            await Click(HOME, 4_000, token).ConfigureAwait(false);
            await Click(X, 1_000, token).ConfigureAwait(false);
            await Click(A, 5_000, token).ConfigureAwait(false); // Closing software prompt
            Log("Closed out of the game!");

            if (addFriends || deleteFriends)
                await DeleteAddFriends(token).ConfigureAwait(false);

            // Open game and select profile
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            Log("Restarting the game!");

            // Switch Logo lag, skip cutscene, game load screen
            await Task.Delay(15_000 + Hub.Config.Raid.ExtraTimeLoadGame, token).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);

            while (!await IsCorrectScreen(CurrentScreen_WildArea, token).ConfigureAwait(false))
                await Task.Delay(2_000, token).ConfigureAwait(false);

            Log("Back in the overworld!");

            // Reconnect to Y-Comm.
            await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);
            Log("Reconnected to Y-Comm!");
        }

        private async Task DeleteAddFriends(CancellationToken token)
        {
            // Get to the profile.
            await Click(DUP, 0_600, token).ConfigureAwait(false);
            for (int i = 1; i < Settings.ProfileNumber; i++)
                await Click(DRIGHT, 0_600, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);

            // Delete before adding to avoid deleting new friends.
            if (deleteFriends)
            {
                Log("Deleting friends.");
                await NavigateFriendsMenu(true, token).ConfigureAwait(false);
                for (int i = 0; i < Settings.NumberFriendsToDelete; i++)
                    await DeleteFriend(token).ConfigureAwait(false);
                EchoUtil.Echo($"Deleted up to {Settings.NumberFriendsToDelete} friends!");
            }

            // If we're deleting friends and need to add friends, it's cleaner to back out 
            // to Home and re-open the profile in case we ran out of friends to delete.
            if (deleteFriends && addFriends)
            {
                Log("Navigating back to add friends.");
                await Click(HOME, 2_000, token).ConfigureAwait(false);
                await Click(DUP, 0_600, token).ConfigureAwait(false);
                for (int i = 1; i < Settings.ProfileNumber; i++)
                    await Click(DRIGHT, 0_600, token).ConfigureAwait(false);
                await Click(A, 2_000, token).ConfigureAwait(false);
            }

            if (addFriends)
            {
                Log("Adding friends.");
                await NavigateFriendsMenu(false, token).ConfigureAwait(false);
                for (int i = 0; i < Settings.NumberFriendsToAdd; i++)
                    await AddFriend(token).ConfigureAwait(false);
                EchoUtil.Echo($"Added up to {Settings.NumberFriendsToAdd} new friends!");
            }

            addFriends = false;
            deleteFriends = false;
            await Click(HOME, 1_000, token).ConfigureAwait(false);
        }

        // Gets us on the friend card if it exists after HOME button has been pressed.
        // Should already be on either "Friend List" or "Add Friend"
        private async Task NavigateFriendsMenu(bool delete, CancellationToken token)
        {
            // Go all the way up, then down 1. Reverse for adding friends.
            if (delete)
            {
                for (int i = 0; i < 4; i++)
                    await Click(DUP, 0_600, token).ConfigureAwait(false);
                await Click(DDOWN, 0_600, token).ConfigureAwait(false);
                await Click(A, 0_800, token).ConfigureAwait(false);

                await NavigateFriends(Settings.RowStartDeletingFriends, 4, token).ConfigureAwait(false);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    await Click(DDOWN, 0_600, token).ConfigureAwait(false);
                await Click(DUP, 0_600, token).ConfigureAwait(false);

                // Click into the menu.
                await Click(A, 0_800, token).ConfigureAwait(false);
                await Click(A, 2_500, token).ConfigureAwait(false);

                await NavigateFriends(Settings.RowStartAddingFriends, 5, token).ConfigureAwait(false);
            }
        }

        // Navigates to the specified row and column.
        private async Task NavigateFriends(int row, int column, CancellationToken token)
        {
            if (row == 1)
                return;

            for (int i = 1; i < row; i++)
                await Click(DDOWN, 0_600, token).ConfigureAwait(false);

            for (int i = 1; i < column; i++)
                await Click(DRIGHT, 0_600, token).ConfigureAwait(false);
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
            await Click(A, 5_000 + Hub.Config.Raid.ExtraTimeDeleteFriend, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
        }

        // Adds one friend. Timing may need to be adjusted since delays vary with connection.
        private async Task AddFriend(CancellationToken token)
        {
            await Click(A, 3_500 + Hub.Config.Raid.ExtraTimeAddFriend, token).ConfigureAwait(false);
            await Click(A, 3_000 + Hub.Config.Raid.ExtraTimeAddFriend, token).ConfigureAwait(false);
        }
    }
}
