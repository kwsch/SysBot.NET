using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule : ModuleBase<SocketCommandContext>
    {
        [Command("blacklist")]
        [Summary("Blacklists mentioned user.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers([Remainder]string _)
        {
            await Process(Context.Message.MentionedUsers.Select(z => z.Id), (z, x) => z.Add(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("unblacklist")]
        [Summary("Un-Blacklists mentioned user.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task UnBlackListUsers([Remainder]string _)
        {
            await Process(Context.Message.MentionedUsers.Select(z => z.Id), (z, x) => z.Remove(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("blacklistId")]
        [Summary("Blacklists IDs. (Useful if user is not in the server).")]
        [RequireOwner]
        public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder]string content)
        {
            await Process(GetIDs(content), (z, x) => z.Add(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("unBlacklistId")]
        [Summary("Un-Blacklists IDs. (Useful if user is not in the server).")]
        [RequireOwner]
        public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder]string content)
        {
            await Process(GetIDs(content), (z, x) => z.Remove(x), z => z.BlacklistedUsers).ConfigureAwait(false);
        }

        [Command("addSudo")]
        [Summary("Adds mentioned user to global sudo")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task SudoUsers([Remainder]string _)
        {
            await Process(Context.Message.MentionedUsers.Select(z => z.Id), (z, x) => z.Add(x), z => z.SudoDiscord).ConfigureAwait(false);
        }

        [Command("removeSudo")]
        [Summary("Removes mentioned user to global sudo")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveSudoUsers([Remainder]string _)
        {
            await Process(Context.Message.MentionedUsers.Select(z => z.Id), (z, x) => z.Remove(x), z => z.SudoDiscord).ConfigureAwait(false);
        }

        [Command("addChannel")]
        [Summary("Adds a channel to the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task AddChannel()
        {
            await Process(new[] {Context.Message.Channel.Id}, (z, x) => z.Add(x), z => z.WhitelistedChannels).ConfigureAwait(false);
        }

        [Command("remiveChannel")]
        [Summary("Removes a channel from the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveChannel()
        {
            await Process(new[] { Context.Message.Channel.Id }, (z, x) => z.Remove(x), z => z.WhitelistedChannels).ConfigureAwait(false);
        }

        private async Task Process(IEnumerable<ulong> values, Func<SensitiveSet<ulong>, ulong, bool> process, Func<DiscordManager, SensitiveSet<ulong>> fetch)
        {
            var mgr = SysCordInstance.Manager;
            var list = fetch(SysCordInstance.Manager);
            var any = false;
            foreach (var v in values)
                any |= process(list, v);

            if (!any)
            {
                await ReplyAsync("Failed.").ConfigureAwait(false);
                return;
            }

            mgr.Write();
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        private static IEnumerable<ulong> GetIDs(string content)
        {
            return content.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
        }
    }
}
