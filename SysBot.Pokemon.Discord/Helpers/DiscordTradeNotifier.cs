using System;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public class DiscordTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private SocketCommandContext Context { get; }
        public Action<PokeRoutineExecutor>? OnFinish { private get; set; }

        public DiscordTradeNotifier(T data, PokeTradeTrainerInfo info, int code, SocketCommandContext context)
        {
            Data = data;
            Info = info;
            Code = code;
            Context = context;
        }

        public void TradeInitialize(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            Context.User.SendMessageAsync($"Initializing trade{receive}. Please be ready. Your code is {Code:0000}.").ConfigureAwait(false);
        }

        public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
            Context.User.SendMessageAsync($"I'm searching for you{trainer}! Your code is {Code:0000}.").ConfigureAwait(false);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            Context.User.SendMessageAsync($"Trade canceled: {msg}").ConfigureAwait(false);
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var message = Data.Species != 0 ? $"Trade finished. Enjoy your {(Species)Data.Species}!" : "Trade finished. Enjoy your Pokemon!";
            Context.User.SendMessageAsync(message).ConfigureAwait(false);
            Context.User.SendPKMAsync(result, "Here's what you traded me!").ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message)
        {
            Context.User.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
