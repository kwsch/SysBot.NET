using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
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

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        LogUtil.LogText(message);
        SendMessage($"@{info.Trainer.TrainerName}: {message}", Settings.NotifyDestination);
    }

    public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        OnFinish?.Invoke(routine);
        var line = $"@{info.Trainer.TrainerName}: Trade abgebrochen, {msg}";
        LogUtil.LogText(line);
        SendMessage(line, Settings.TradeCanceledDestination);
    }

    public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        OnFinish?.Invoke(routine);
        var tradedToUser = Data.Species;
        var message = $"@{info.Trainer.TrainerName}: " + (tradedToUser != 0 ? $"Handel abgeschlossen. Viel spass mit deinem {(Species)tradedToUser}!" : "Handel beendet!");
        LogUtil.LogText(message);
        SendMessage(message, Settings.TradeFinishDestination);
    }

    public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
        var msg = $"@{info.Trainer.TrainerName} (ID: {info.ID}): Starte Handel{receive} mit dir. Bitte sei Bereit. Benutze den Code den du mir zugeflüstert hast!";
        var dest = Settings.TradeStartDestination;
        if (dest == TwitchMessageDestination.Whisper)
            msg += $" Dein HandelsCode ist: {info.Code:0000 0000}";
        LogUtil.LogText(msg);
        SendMessage(msg, dest);
    }

    public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var name = Info.TrainerName;
        var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", @{name}";
        var message = $"Ich warte auf dich, {trainer}! Mein IGN ist {routine.InGameName}.";
        var dest = Settings.TradeSearchDestination;
        if (dest == TwitchMessageDestination.Channel)
            message += " Benutze den Code den du mir zugeflüstert hast!";
        else if (dest == TwitchMessageDestination.Whisper)
            message += $" Dein HandelsCode ist: {info.Code:0000 0000}";
        LogUtil.LogText(message);
        SendMessage($"@{info.Trainer.TrainerName} {message}", dest);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
    {
        var msg = message.Summary;
        if (message.Details.Count > 0)
            msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
        LogUtil.LogText(msg);
        SendMessage(msg, Settings.NotifyDestination);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        var msg = $"Details for {result.FileName}: " + message;
        LogUtil.LogText(msg);
        SendMessage(msg, Settings.NotifyDestination);
    }

    private void SendMessage(string message, TwitchMessageDestination dest)
    {
        switch (dest)
        {
            case TwitchMessageDestination.Channel:
                Client.SendMessage(Channel, message);
                break;
            case TwitchMessageDestination.Whisper:
                Client.SendWhisper(Username, message);
                break;
        }
    }
}
