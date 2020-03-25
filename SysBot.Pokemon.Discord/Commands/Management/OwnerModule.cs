using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.Pokemon.Discord
{
    public class OwnerModule : SudoModule
    {
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

        [Command("removeChannel")]
        [Summary("Removes a channel from the list of channels that are accepting commands.")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task RemoveChannel()
        {
            await Process(new[] { Context.Message.Channel.Id }, (z, x) => z.Remove(x), z => z.WhitelistedChannels).ConfigureAwait(false);
        }

        [Command("sudoku")]
        [Summary("Causes the entire process to end itself!")]
        [RequireOwner]
        // ReSharper disable once UnusedParameter.Global
        public async Task ExitProgram()
        {
            await ReplyAsync("Shutting down... goodbye!").ConfigureAwait(false);
            Environment.Exit(0);
        }
    }
}
