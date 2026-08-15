using System;
using System.Linq;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using TwitchLib.Client;

namespace SysBot.Pokemon.Twitch;

public class TwitchTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
{
    private T Data { get; }
    private PokeTradeTrainerInfo Info { get; }
    private int Code { get; }
    private string Username { get; }
    private TwitchClient Client { get; }
    private string Channel { get; }
    private TwitchSettings Settings { get; }

    public TwitchTradeNotifier(T data, PokeTradeTrainerInfo info, int code, string username, TwitchClient client, string channel, TwitchSettings settings)
    {
        Data = data;
        Info = info;
        Code = code;
        Username = username;
        Client = client;
        Channel = channel;
        Settings = settings;

        LogUtil.LogText($"Created trade details for {Username} - {Code}");
    }

    public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }

    public async Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        LogUtil.LogText(message);
        await SendMessage($"@{info.Trainer.TrainerName}: {message}", Settings.NotifyDestination).ConfigureAwait(false);
    }

    public async Task TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        OnFinish?.Invoke(routine);
        var line = $"@{info.Trainer.TrainerName}: Trade canceled, {msg}";
        LogUtil.LogText(line);
        await SendMessage(line, Settings.TradeCanceledDestination).ConfigureAwait(false);
    }

    public async Task TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        OnFinish?.Invoke(routine);
        var tradedToUser = Data.Species;
        var message = $"@{info.Trainer.TrainerName}: " + (tradedToUser != 0 ? $"Trade finished. Enjoy your {(Species)tradedToUser}!" : "Trade finished!");
        LogUtil.LogText(message);
        await SendMessage(message, Settings.TradeFinishDestination).ConfigureAwait(false);
    }

    public async Task TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
        var msg = $"@{info.Trainer.TrainerName} (ID: {info.Id}): Initializing trade{receive} with you. Please be ready. Use the code you whispered me to search!";
        var dest = Settings.TradeStartDestination;
        if (dest == TwitchMessageDestination.Whisper)
            msg += $" Your trade code is: {info.Code:0000 0000}";
        LogUtil.LogText(msg);
        await SendMessage(msg, dest).ConfigureAwait(false);
    }

    public async Task TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var name = Info.TrainerName;
        var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", @{name}";
        var message = $"I'm waiting for you{trainer}! My IGN is {routine.InGameName}.";
        var dest = Settings.TradeSearchDestination;
        if (dest == TwitchMessageDestination.Channel)
            message += " Use the code you whispered me to search!";
        else if (dest == TwitchMessageDestination.Whisper)
            message += $" Your trade code is: {info.Code:0000 0000}";
        LogUtil.LogText(message);
        await SendMessage($"@{info.Trainer.TrainerName} {message}", dest).ConfigureAwait(false);
    }

    public async Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary trade)
    {
        var msg = trade.Summary;
        if (trade.Details.Count > 0)
            msg += ", " + string.Join(", ", trade.Details.Select(z => $"{z.Heading}: {z.Detail}"));
        LogUtil.LogText(msg);
        await SendMessage(msg, Settings.NotifyDestination);
    }

    public async Task SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        var msg = $"Details for {result.FileName}: " + message;
        LogUtil.LogText(msg);
        await SendMessage(msg, Settings.NotifyDestination).ConfigureAwait(false);
    }

    private async Task SendMessage(string message, TwitchMessageDestination dest)
    {
        switch (dest)
        {
            case TwitchMessageDestination.Channel:
                await Client.SendMessageAsync(Channel, message).ConfigureAwait(false);
                break;
            case TwitchMessageDestination.Whisper:
                await Client.SendMessageAsync(Channel, $"/w {Username} {message}").ConfigureAwait(false);
                break;
        }
    }
}
