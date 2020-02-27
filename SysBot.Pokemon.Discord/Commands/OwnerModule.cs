using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        public async Task<bool> IsOwner(IUser author)
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            return author.Id.Equals(app.Owner.Id);
        }

        [Command("blacklist")]
        [Summary("Blacklists mentioned user.")]
        public async Task BlackListUsers()
        {
            if (!await IsOwner(Context.Message.Author).ConfigureAwait(false))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var users = Context.Message.MentionedUsers;
            var userids = users.Select(z => z.Id.ToString());
            var blacklist = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.DiscordBlackList);
            blacklist.AddRange(userids);
            SysCordInstance.Self.Hub.Config.DiscordBlackList = string.Join(", ", new HashSet<string>(blacklist)); // unique values
            await ReplyAsync("Blacklisted mentioned users from using the bot!").ConfigureAwait(false);
        }

        [Command("unblacklist")]
        [Summary("Un-Blacklists mentioned user.")]
        public async Task UnBlackListUsers()
        {
            if (!await IsOwner(Context.Message.Author).ConfigureAwait(false))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var users = Context.Message.MentionedUsers.Select(z => z.Id).ToList();
            var blusers = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.DiscordBlackList);
            var iter = blusers.ToList(); // Deepclone
            foreach (var ch in iter)
            {
                if (!ulong.TryParse(ch, out var uid) || !users.Contains(uid))
                    continue;
                blusers.Remove(uid.ToString());
            }

            SysCordInstance.Self.Hub.Config.DiscordBlackList = string.Join(", ", blusers);
            await ReplyAsync("Un-Blacklisted mentioned users from using the bot!").ConfigureAwait(false);
        }

        [Command("blacklistId")]
        [Summary("Blacklists IDs. (Useful if user is not in the server).")]
        public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder]string content)
        {
            if (!await IsOwner(Context.Message.Author).ConfigureAwait(false))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var users = content.Split(new[] {",", ", ", " "}, StringSplitOptions.RemoveEmptyEntries);
            var userids = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.DiscordBlackList);
            foreach (var user in users)
            {
                if (!ulong.TryParse(user, out var uid))
                    continue;
                userids.Add(uid.ToString());
            }

            SysCordInstance.Self.Hub.Config.DiscordBlackList = string.Join(", ", new HashSet<string>(userids)); // empty string joins
            await ReplyAsync("Blacklisted listed IDs from using the bot!").ConfigureAwait(false);
        }

        [Command("unBlacklistId")]
        [Summary("Un-Blacklists IDs. (Useful if user is not in the server).")]
        public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder]string content)
        {
            if (!await IsOwner(Context.Message.Author).ConfigureAwait(false))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var users = content.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries);
            var userids = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.DiscordBlackList);
            var iter = userids.ToList(); // Deepcopy
            foreach (var user in iter)
            {
                if (!ulong.TryParse(user, out var uid) || !users.Contains(uid.ToString()))
                    continue;
                userids.Remove(uid.ToString());
            }

            SysCordInstance.Self.Hub.Config.DiscordBlackList = string.Join(", ", new HashSet<string>(userids)); // empty string joins
            await ReplyAsync("Un-Blacklisted listed IDs from using the bot!").ConfigureAwait(false);
        }

        [Command("addSudo")]
        [Summary("Adds mentioned user to global sudo")]
        public async Task SudoUsers()
        {
            if (!await IsOwner(Context.Message.Author).ConfigureAwait(false))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var users = Context.Message.MentionedUsers;
            var userids = users.Select(z => z.Id.ToString());
            var sudos = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.GlobalSudoList);
            sudos.AddRange(userids);
            SysCordInstance.Self.Hub.Config.GlobalSudoList = string.Join(", ", new HashSet<string>(sudos)); // unique values
            await ReplyAsync("Mentioned users are now sudo users for the bot!").ConfigureAwait(false);
        }

        [Command("removeSudo")]
        [Summary("Removes mentioned user to global sudo")]
        public async Task RemoveSudoUsers()
        {
            if (!await IsOwner(Context.Message.Author).ConfigureAwait(false))
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var users = Context.Message.MentionedUsers;
            var userids = users.Select(z => z.Id.ToString()).ToList();
            var sudos = ReusableActions.GetListFromString(SysCordInstance.Self.Hub.Config.GlobalSudoList);
            var iter = sudos.ToList(); // Deepclone
            foreach (var ch in iter)
            {
                if (!ulong.TryParse(ch, out var uid) || !userids.Contains(uid.ToString()))
                    continue;
                sudos.Remove(uid.ToString());
            }

            SysCordInstance.Self.Hub.Config.GlobalSudoList = string.Join(", ", new HashSet<string>(sudos)); // unique values
            await ReplyAsync("Mentioned users are no longer sudo users for the bot!").ConfigureAwait(false);
        }
    }
}
