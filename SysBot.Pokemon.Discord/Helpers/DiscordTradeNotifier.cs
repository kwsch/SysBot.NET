using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public sealed record DiscordTradeNotifier<T>(T Data, PokeTradeTrainerInfo Info, int Code, SocketInteractionContext Trader)
    : IPokeTradeNotifier<T>
    where T : PKM, new()
{
    private T Data { get; } = Data;
    private PokeTradeTrainerInfo Info { get; } = Info;
    private int Code { get; } = Code;
    private SocketInteractionContext Trader { get; } = Trader;
    public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }
    public readonly PokeTradeHub<T> Hub = SysCord<T>.Runner.Hub;

    public async Task TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
        var message = $"Initializing trade{receive}. Please be ready. Your code is **{Code:0000 0000}**.";

        await SendNotification(message).ConfigureAwait(false);
    }

    public async Task TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var name = Info.TrainerName;
        var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
        var message = $"I'm waiting for you{trainer}! Your code is **{Code:0000 0000}**. My IGN is **{routine.InGameName}**.";

        await SendNotification(message).ConfigureAwait(false);
    }

    public async Task TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        OnFinish?.Invoke(routine);
        var message = $"Trade canceled: {msg}";

        await SendNotification(message).ConfigureAwait(false);
    }

    public async Task TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        OnFinish?.Invoke(routine);
        var tradedToUser = Data.Species;
        var message = tradedToUser != 0 ? $"Trade finished. Enjoy your {(Species)tradedToUser}!" : "Trade finished!";

        await SendNotification(message).ConfigureAwait(false);
        if (result.Species != 0 && Hub.Config.Discord.ReturnPKMs)
            await Trader.SendFilePrivatelyAsync(result, "Here's what you traded me!").ConfigureAwait(false);
    }

    public async Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        => await SendNotification(message).ConfigureAwait(false);
    private async Task SendNotification(string message, Embed? embed = null)
    {
        // Discord makes all interaction modals stale after 15 minutes.
        // Depending on how long we take to start and complete the trade (queued users), this might be called >= 15 minutes after command issued.
        // So, we do the standard behavior: direct message the user.
        await Trader.Interaction.User.SendMessageAsync(message, embed: embed).ConfigureAwait(false);
    }

    public async Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary trade)
    {
        if (trade.ExtraInfo is SeedSearchResult r)
        {
            await SendNotificationZ3(r).ConfigureAwait(false);
            return;
        }

        var message = trade.Summary;
        if (trade.Details.Count > 0)
            message += ", " + string.Join(", ", trade.Details.Select(z => $"{z.Heading}: {z.Detail}"));

        await SendNotification(message).ConfigureAwait(false);
    }

    public async Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        if (result.Species != 0 && (Hub.Config.Discord.ReturnPKMs || info.Type == PokeTradeType.Dump))
            await Trader.SendFilePrivatelyAsync(result, message).ConfigureAwait(false);
    }

    private async Task SendNotificationZ3(SeedSearchResult searchResult)
    {
        var message = $"Here are the details for `{searchResult.Seed:X16}`:";
        var embed = new EmbedBuilder { Color = Color.LighterGrey };
        embed.AddField(x =>
        {
            x.Name = $"Seed: {searchResult.Seed:X16}";
            x.Value = searchResult.ToString();
            x.IsInline = false;
        });

        // Seed check might be more than 15 minutes stale. Just DM them, not like the public needs to see their seeds.
        await SendNotification(message, embed: embed.Build()).ConfigureAwait(false);
    }
}
