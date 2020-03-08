using System;
using System.Linq;
using PKHeX.Core;
using SysBot.Base;
using TwitchLib.Client;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private string Username { get; }
        private TwitchClient Client { get; }
        private string Channel { get; }

        public TwitchTradeNotifier(T data, PokeTradeTrainerInfo info, int code, string username, TwitchClient client, string channel)
        {
            Data = data;
            Info = info;
            Code = code;
            Username = username;
            Client = client;
            Channel = channel;

            Console.WriteLine($"{Username} - {Code}");
        }

        public Action<PokeRoutineExecutor> OnFinish { private get; set; }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message)
        {
            // Client.SendMessage(Channel, message);
            LogUtil.LogText(message);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            var line = $"Trade canceled: {msg}";
            // Client.SendMessage(Channel, line);
            LogUtil.LogText(line);
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = tradedToUser != 0 ? $"Trade finished. Enjoy your {(Species)tradedToUser}!" : "Trade finished!";
            // Client.SendMessage(Channel, message);
            LogUtil.LogText(message);
        }

        public void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            Client.SendMessage(Channel, $"Initializing trade{receive} with you, {info.Trainer.TrainerName} (ID: {info.ID}). Please be ready. Use the code you whispered me to search!");
        }

        public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
            var message = $"I'm waiting for you{trainer}! My IGN is {routine.InGameName}. Use the code you whispered me to search!";
            // Turn on if the bot is verified, else you will get rate-limited @ 40 whispers / day
            // Client.SendWhisper(Username, msg);
            LogUtil.LogText(message);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            Client.SendMessage(Channel, msg);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result, string message)
        {
            Client.SendMessage(Channel, $"Details for {result.FileName}");
            Client.SendMessage(Channel, message);
        }
    }
}
