using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[Group("control", "Commands related to controlling the console itself.")]
[DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
[RequireRoleAccess(PokeRoutineType.RemoteControl)]
[RequireContext(ContextType.Guild)]
public class RemoteControlModule<T> : SlashModuleBase where T : PKM, new()
{
    [SlashCommand("click", "Clicks the specified button.")]
    public async Task ClickAsync(
        [Summary(nameof(button), "The button to press.")] SwitchButton button)
    {
        var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await RespondAsync($"No bot is available to execute your command: {button}").ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(button, bot).ConfigureAwait(false);
    }

    [SlashCommand("click-ip", "Clicks a button on a specific bot.")]
    public async Task ClickAsync(
        [Summary(nameof(ip), "Which bot to perform the command on.")] string ip,
        [Summary(nameof(button), "The button to press.")] SwitchButton button)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot is available to execute your command: {button}").ConfigureAwait(false);
            return;
        }

        await ClickAsyncImpl(button, bot).ConfigureAwait(false);
    }

    [SlashCommand("set-stick", "Sets the stick to the specified position.")]
    public async Task SetStickAsync(
        [Summary(nameof(stick), "Which control stick to adjust.")] SwitchStick stick = SwitchStick.LEFT,
        [Summary(nameof(x), "The X position of the stick angle.")] short x = 0,
        [Summary(nameof(y), "The Y position of the stick angle.")] short y = 0,
        [Summary(nameof(ms), "The duration to hold the stick in the position. Leave blank for infinite duration until changed")] ushort? ms = 1000)
    {
        var bot = SysCord<T>.Runner.Bots.Find(z => IsRemoteControlBot(z.Bot));
        if (bot == null)
        {
            await RespondAsync($"No bot is available to execute your command: {stick}").ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(stick, x, y, ms, bot).ConfigureAwait(false);
    }

    [SlashCommand("set-stick-ip", "Sets a stick on a specific bot.")]
    public async Task SetStickAsync(
        [Summary(nameof(ip), "Which bot to perform the command on.")] string ip,
        [Summary(nameof(stick), "Which control stick to adjust.")] SwitchStick stick = SwitchStick.LEFT,
        [Summary(nameof(x), "The X position of the stick angle.")] short x = 0,
        [Summary(nameof(y), "The Y position of the stick angle.")] short y = 0,
        [Summary(nameof(ms), "The duration to hold the stick in the position. Leave blank for infinite duration until changed.")] ushort? ms = 1000)
    {
        var bot = SysCord<T>.Runner.GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        await SetStickAsyncImpl(stick, x, y, ms, bot).ConfigureAwait(false);
    }

    [SlashCommand("screen", "Updates the console screen powered-on state.")]
    public Task SetScreenAsync(
        [Summary(nameof(ip), "Which bot to perform the command on.")] string ip,
        [Summary(nameof(on), "The desired screen powered-on state.")] bool on = true)
        => SetScreenGuarded(on, ip);

    private async Task SetScreenGuarded(bool on, string ip)
    {
        var bot = GetBot(ip);
        if (bot == null)
        {
            await RespondAsync($"No bot has that IP address ({ip}).").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        var cmd = SwitchCommand.SetScreen(on ? ScreenState.On : ScreenState.Off, crlf);
        await b.Connection.SendAsync(cmd).ConfigureAwait(false);
        await RespondAsync("Screen state set to: " + (on ? "On" : "Off")).ConfigureAwait(false);
    }

    private async Task ClickAsyncImpl(SwitchButton button, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(button))
        {
            await RespondAsync($"Unknown button value: {button}").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.Click(button, crlf)).ConfigureAwait(false);
        await RespondAsync($"{b.Connection.Name} has performed: {button}").ConfigureAwait(false);
    }

    private async Task SetStickAsyncImpl(SwitchStick s, short x, short y, ushort? ms, BotSource<PokeBotState> bot)
    {
        if (!Enum.IsDefined(s))
        {
            await RespondAsync($"Unknown stick: {s}").ConfigureAwait(false);
            return;
        }

        var b = bot.Bot;
        var crlf = b is SwitchRoutineExecutor<PokeBotState> { UseCRLF: true };
        await b.Connection.SendAsync(SwitchCommand.SetStick(s, x, y, crlf)).ConfigureAwait(false);
        if (ms is not { } value)
        {
            await RespondAsync($"{b.Connection.Name} has performed: {s} and will hold the position until changed.").ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        await Task.Delay(value).ConfigureAwait(false);
        await b.Connection.SendAsync(SwitchCommand.ResetStick(s, crlf)).ConfigureAwait(false);
        await FollowupAsync($"{b.Connection.Name} has performed: {s} and reset the stick position.").ConfigureAwait(false);
    }

    private static BotSource<PokeBotState>? GetBot(string ip)
    {
        var r = SysCord<T>.Runner;
        return r.GetBot(ip) ?? r.Bots.Find(x => x.IsRunning);
    }

    private static bool IsRemoteControlBot(RoutineExecutor<PokeBotState> b) => b is RemoteControlBotSWSH or RemoteControlBotBS or RemoteControlBotLA or RemoteControlBotSV or RemoteControlBotLZA;
}
