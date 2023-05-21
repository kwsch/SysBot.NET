using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public class SudoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        [Command("blacklist")]
        [Summary("Blacklists a mentioned Discord user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistComment")]
        [Summary("Adds a comment for a blacklisted Discord user ID.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers(ulong id, [Remainder] string comment)
        {
            var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
            if (obj is null)
            {
                await ReplyAsync($"Unable to find a user with that ID ({id}).").ConfigureAwait(false);
                return;
            }

            var oldComment = obj.Comment;
            obj.Comment = comment;
            await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
        }

        [Command("unblacklist")]
        [Summary("Removes a mentioned Discord user from the blacklist.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task UnBlackListUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.RemoveAll(z => objects.Any(o => o.ID == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistId")]
        [Summary("Blacklists Discord user IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            var objects = IDs.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("unBlacklistId")]
        [Summary("Removes Discord user IDs from the blacklist. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            SysCordSettings.Settings.UserBlacklist.RemoveAll(z => IDs.Any(o => o == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistSummary")]
        [Alias("printBlacklist", "blacklistPrint")]
        [Summary("Prints the list of blacklisted Discord users.")]
        [RequireSudo]
        public async Task PrintBlacklist()
        {
            var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
            var msg = string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        [Command("banID")]
        [Summary("Bans online user IDs.")]
        [RequireSudo]
        public async Task BanOnlineIDs([Summary("Comma Separated Online IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            var objects = IDs.Select(GetReference);

            var me = SysCord<T>.Runner;
            var hub = me.Hub;
            hub.Config.TradeAbuse.BannedIDs.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("bannedIDComment")]
        [Summary("Adds a comment for a banned online user ID.")]
        [RequireSudo]
        public async Task BanOnlineIDs(ulong id, [Remainder] string comment)
        {
            var me = SysCord<T>.Runner;
            var hub = me.Hub;
            var obj = hub.Config.TradeAbuse.BannedIDs.List.Find(z => z.ID == id);
            if (obj is null)
            {
                await ReplyAsync($"Unable to find a user with that online ID ({id}).").ConfigureAwait(false);
                return;
            }

            var oldComment = obj.Comment;
            obj.Comment = comment;
            await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
        }

        [Command("unbanID")]
        [Summary("Bans online user IDs.")]
        [RequireSudo]
        public async Task UnBanOnlineIDs([Summary("Comma Separated Online IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            var objects = IDs.Select(GetReference);

            var me = SysCord<T>.Runner;
            var hub = me.Hub;
            hub.Config.TradeAbuse.BannedIDs.RemoveAll(z => IDs.Any(o => o == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("bannedIDSummary")]
        [Alias("printBannedID", "bannedIDPrint")]
        [Summary("Prints the list of banned online IDs.")]
        [RequireSudo]
        public async Task PrintBannedOnlineIDs()
        {
            var me = SysCord<T>.Runner;
            var hub = me.Hub;
            var lines = hub.Config.TradeAbuse.BannedIDs.Summarize();
            var msg = string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        [Command("forgetUser")]
        [Alias("forget")]
        [Summary("Forgets users that were previously encountered.")]
        [RequireSudo]
        public async Task ForgetPreviousUser([Summary("Comma Separated Online IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            var objects = IDs.Select(GetReference);

            foreach (var ID in IDs)
            {
                PokeRoutineExecutorBase.PreviousUsers.RemoveAll(ID);
                PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAll(ID);
            }
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("previousUserSummary")]
        [Alias("prevUsers")]
        [Summary("Prints a list of previously encountered users.")]
        [RequireSudo]
        public async Task PrintPreviousUsers()
        {
            bool found = false;
            var lines = PokeRoutineExecutorBase.PreviousUsers.Summarize();
            if (lines.Any())
            {
                found = true;
                var msg = "Previous Users:\n" + string.Join("\n", lines);
                await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
            }

            lines = PokeRoutineExecutorBase.PreviousUsersDistribution.Summarize();
            if (lines.Any())
            {
                found = true;
                var msg = "Previous Distribution Users:\n" + string.Join("\n", lines);
                await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
            }
            if (!found)
                await ReplyAsync("No previous users found.").ConfigureAwait(false);
        }

        private RemoteControlAccess GetReference(IUser channel) => new()
        {
            ID = channel.Id,
            Name = channel.Username,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        private RemoteControlAccess GetReference(ulong id) => new()
        {
            ID = id,
            Name = "Manual",
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        protected static IEnumerable<ulong> GetIDs(string content)
        {
            return content.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
        }
    }
}