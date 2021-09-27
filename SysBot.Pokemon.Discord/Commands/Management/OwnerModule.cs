using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule : SudoModule
    {
        [Command("addSudo")]
        [Summary("Adds mentioned user to global sudo")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task SudoUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("removeSudo")]
        [Summary("Removes mentioned user from global sudo")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveSudoUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("addChannel")]
        [Summary("Adds a channel to the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task AddChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.AddIfNew(new[] { obj });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("removeChannel")]
        [Summary("Removes a channel from the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveChannel()
        {
            var obj = GetReference(Context.Message.Channel);
            SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("sudoku")]
        [Alias("kill", "shutdown")]
        [Summary("Causes the entire process to end itself!")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task ExitProgram()
        {
            await Context.Channel.EchoAndReply("Shutting down... goodbye! **Bot services are going offline.**").ConfigureAwait(false);
            Environment.Exit(0);
        }

        private RemoteControlAccess GetReference(IUser channel) => new()
        {
            ID = channel.Id,
            Name = channel.Username,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        private RemoteControlAccess GetReference(IChannel channel) => new()
        {
            ID = channel.Id,
            Name = channel.Name,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };
    }
}
