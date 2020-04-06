using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    [Summary("Remotely controls a bot.")]
    public class RemoteControlModule : ModuleBase<SocketCommandContext>
    {
        [Command("click")]
        [Summary("Clicks the specified button.")]
        [RequireQueueRole(nameof(DiscordManager.RolesRemoteControl))]
        public async Task ClickAsync(SwitchButton b)
        {
            if (!Enum.IsDefined(typeof(SwitchButton), b))
            {
                await ReplyAsync($"Unknown button value: {b}").ConfigureAwait(false);
                return;
            }

            var bot = SysCordInstance.Runner.Bots.Find(z => z.Bot is RemoteControlBot);
            if (bot == null)
            {
                await ReplyAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
                return;
            }

            await bot.Bot.Connection.SendAsync(SwitchCommand.Click(b), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{bot.Bot.Connection.Name} has performed: {b}").ConfigureAwait(false);
        }

        [Command("setStick")]
        [Summary("Sets the stick to the specified position.")]
        [RequireQueueRole(nameof(DiscordManager.RolesRemoteControl))]
        public async Task SetStickAsync(SwitchStick s, short x, short y, ushort ms = 1_000)
        {
            if (!Enum.IsDefined(typeof(SwitchStick), s))
            {
                await ReplyAsync($"Unknown stick: {s}").ConfigureAwait(false);
                return;
            }

            var bot = SysCordInstance.Runner.Bots.Find(z => z.Bot is RemoteControlBot);
            if (bot == null)
            {
                await ReplyAsync($"No bot is available to execute your command: {s}").ConfigureAwait(false);
                return;
            }

            await bot.Bot.Connection.SendAsync(SwitchCommand.SetStick(s,x,y), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{bot.Bot.Connection.Name} has performed: {s}").ConfigureAwait(false);
            await Task.Delay(ms).ConfigureAwait(false);
            await bot.Bot.Connection.SendAsync(SwitchCommand.ResetStick(s), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{bot.Bot.Connection.Name} has reset the stick position.").ConfigureAwait(false);
        }
    }
}
