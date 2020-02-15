using System;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public class DiscordTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private SocketCommandContext Context { get; }
        public Action? OnFinish { private get; set; }

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
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", ({name})";
            Context.User.SendMessageAsync($"I'm searching for you{trainer}! Your code is {Code:0000}.").ConfigureAwait(false);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            Context.User.SendMessageAsync($"Trade has been canceled: {msg}").ConfigureAwait(false);
            OnFinish?.Invoke();
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            var message = Data.Species != 0 ? $"Trade has been finished. Enjoy your {(Species)Data.Species}!" : "Trade has been finished. Enjoy your Pokemon!";
            Context.User.SendMessageAsync(message).ConfigureAwait(false);
            Context.User.SendPKMAsync(result, "Here's what you traded me!").ConfigureAwait(false);
            OnFinish?.Invoke();
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message)
        {
            Context.User.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}