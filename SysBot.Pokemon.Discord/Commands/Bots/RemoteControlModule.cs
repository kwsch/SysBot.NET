﻿using Discord.Commands;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            var bot = SysCordInstance.Runner.Bots.Find(z => z.Bot is RemoteControlBot);
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
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot is available to execute your command: {b}").ConfigureAwait(false);
                return;
            }

            await ClickAsyncImpl(b, bot).ConfigureAwait(false);
        }

        [Command("setStick")]
        [Summary("Sets the stick to the specified position.")]
        [RequireQueueRole(nameof(DiscordManager.RolesRemoteControl))]
        public async Task SetStickAsync(SwitchStick s, short x, short y, ushort ms = 1_000)
        {
            var bot = SysCordInstance.Runner.Bots.Find(z => z.Bot is RemoteControlBot);
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
            var bot = SysCordInstance.Runner.GetBot(ip);
            if (bot == null)
            {
                await ReplyAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
                return;
            }

            await SetStickAsyncImpl(s, x, y, ms, bot).ConfigureAwait(false);
        }

        private async Task ClickAsyncImpl(SwitchButton b, BotSource<PokeBotConfig> bot)
        {
            if (!Enum.IsDefined(typeof(SwitchButton), b))
            {
                await ReplyAsync($"Unknown button value: {b}").ConfigureAwait(false);
                return;
            }

            await bot.Bot.Connection.SendAsync(SwitchCommand.Click(b), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{bot.Bot.Connection.Name} has performed: {b}").ConfigureAwait(false);
        }

        private async Task SetStickAsyncImpl(SwitchStick s, short x, short y, ushort ms, BotSource<PokeBotConfig> bot)
        {
            if (!Enum.IsDefined(typeof(SwitchStick), s))
            {
                await ReplyAsync($"Unknown stick: {s}").ConfigureAwait(false);
                return;
            }

            await bot.Bot.Connection.SendAsync(SwitchCommand.SetStick(s, x, y), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{bot.Bot.Connection.Name} has performed: {s}").ConfigureAwait(false);
            await Task.Delay(ms).ConfigureAwait(false);
            await bot.Bot.Connection.SendAsync(SwitchCommand.ResetStick(s), CancellationToken.None).ConfigureAwait(false);
            await ReplyAsync($"{bot.Bot.Connection.Name} has reset the stick position.").ConfigureAwait(false);
        }
    }
}
