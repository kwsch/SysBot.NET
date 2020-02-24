using System;
using PKHeX.Core;
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
            Client.SendMessage(Channel, message);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            Client.SendMessage(Channel, $"Trade canceled: {msg}");
            OnFinish?.Invoke(routine);
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            var message = Data.Species != 0 ? $"Trade finished {Username}. Enjoy your {(Species)Data.Species}!" : "Trade finished. Enjoy your Pokemon!";
            Client.SendMessage(Channel, message);
            OnFinish?.Invoke(routine);
        }

        public void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            Client.SendMessage(Channel, $"Initializing trade{receive} with you, {info.Trainer.TrainerName}. Please be ready. Use the code you whispered me to search!");
        }

        public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
            Client.SendWhisper(Username, $"I'm searching for you{trainer}! Use the code you whispered me to search!");
        }
    }
}
