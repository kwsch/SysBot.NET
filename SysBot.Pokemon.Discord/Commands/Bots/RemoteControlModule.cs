using Discord.Commands;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Remotely controls a bot.")]
    public class RemoteControlModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        [Command("click")]
        [Summary("Clicks the specified button.")]
        [RequireRoleAccess(nameof(DiscordManager.RolesRemoteControl))]
        public async Task ClickAsync(SwitchButton b)
        {
            var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
            if (bot == null)
            {
                await ReplyAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
                return;
            }

            await ClickAsyncImpl(b, bot).ConfigureAwait(false);
        }

        [Command("click")]
        [Summary("Clicks the specified button.")]
        [RequireSudo]
        public async Task ClickAsync(string ip, SwitchButton b)
        {
            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
                return;
            }

            await ClickAsyncImpl(b, bot).ConfigureAwait(false);
        }

        [Command("setStick")]
        [Summary("Sets the stick to the specified position.")]
        [RequireRoleAccess(nameof(DiscordManager.RolesRemoteControl))]
        public async Task SetStickAsync(SwitchStick s, short x, short y, ushort ms = 1_000)
        {
            var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
            if (bot == null)
            {
                await ReplyAsync($"No bot is available to execute your command: {s}").ConfigureAwait(false);
                return;
            }

            await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
        }

        [Command("setStick")]
        [Summary("Sets the stick to the specified position.")]
        [RequireSudo]
        public async Task SetStickAsync(string ip, SwitchStick s, short x, short y, ushort ms = 1_000)
        {
            var bot = SysCord<T>.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
        }

        [Command("setScreenOn")]
        [Alias("screenOn", "scrOn")]
        [Summary("Turns the screen on")]
        [RequireSudo]
        public async Task SetScreenOnAsync([Remainder] string ip)
        {
            await SetScreen(true, ip).ConfigureAwait(false);
        }

        [Command("setScreenOff")]
        [Alias("screenOff", "scrOff")]
        [Summary("Turns the screen off")]
        [RequireSudo]
        public async Task SetScreenOffAsync([Remainder] string ip)
        {
            await SetScreen(false, ip).ConfigureAwait(false);
        }

        private async Task SetScreen(bool on, string ip)
        {
            var bot = GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            var b = bot.Bot;
            var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
            await b.Connection.SendAsync(SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync("Screen state set to: " + (on ? "On" : "Off")).ConfigureAwait(false);
        }

        private static BotSource<PokeBotState>? GetBot(string ip)
        {
            var r = SysCord<T>.Runner;
            return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning); // safe fallback for users who mistype IP address for single bot instances
        }

        private async Task ClickAsyncImpl(SwitchButton button,BotSource<PokeBotState> bot)
        {
            if (!Enum.IsDefined(typeof(SwitchButton), button))
            {
                await ReplyAsync($"Unknown button value: {button}").ConfigureAwait(false);
                return;
            }

            var b = bot.Bot;
            var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
            await b.Connection.SendAsync(SwitchCommand.Click(button, crlf), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{b.Connection.Name} has performed: {button}").ConfigureAwait(false);
        }

        private async Task SetStickAsyncImpl(SwitchStick s, short x, short y, ushort ms,BotSource<PokeBotState> bot)
        {
            if (!Enum.IsDefined(typeof(SwitchStick), s))
            {
                await ReplyAsync($"Unknown stick: {s}").ConfigureAwait(false);
                return;
            }

            var b = bot.Bot;
            var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
            await b.Connection.SendAsync(SwitchCommand.SetStick(s, x, y, crlf), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{b.Connection.Name} has performed: {s}").ConfigureAwait(false);
            await Task.Delay(ms).ConfigureAwait(false);
            await b.Connection.SendAsync(SwitchCommand.ResetStick(s, crlf), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{b.Connection.Name} has reset the stick position.").ConfigureAwait(false);
        }

        private bool IsRemoteControlBot(RoutineExecutor<PokeBotState> botstate)
            => botstate is RemoteControlBot or RemoteControlBotBS or RemoteControlBotLA or RemoteControlBotSV;
    }
}
