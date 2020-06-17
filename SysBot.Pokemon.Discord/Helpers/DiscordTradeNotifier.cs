using System;
using System.Linq;
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
        public PokeTradeHub<PK8> Hub = SysCordInstance.Self.Hub;

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
            Context.User.SendMessageAsync($"Initializing trade{receive}. Please be ready. Your code is **{Code:0000 0000}**.").ConfigureAwait(false);
        }

        public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", {name}";
            Context.User.SendMessageAsync($"I'm waiting for you{trainer}! Your code is **{Code:0000 0000}**. My IGN is **{routine.InGameName}**.").ConfigureAwait(false);
        }

        public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            Context.User.SendMessageAsync($"Trade canceled: {msg}").ConfigureAwait(false);
        }

        public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = tradedToUser != 0 ? $"Trade finished. Enjoy your {(Species)tradedToUser}!" : "Trade finished!";
            Context.User.SendMessageAsync(message).ConfigureAwait(false);
            if (result.Species != 0 && Hub.Config.Discord.ReturnPK8s)
                Context.User.SendPKMAsync(result, "Here's what you traded me!").ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, string message)
        {
            Context.User.SendMessageAsync(message).ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            if (message.ExtraInfo is SeedSearchResult r)
            {
                SendNotificationZ3(r);
                return;
            }

            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            Context.User.SendMessageAsync(msg).ConfigureAwait(false);
        }

        public void SendNotification(PokeRoutineExecutor routine, PokeTradeDetail<T> info, T result, string message)
        {
            if (result.Species != 0 && (Hub.Config.Discord.ReturnPK8s || info.Type == PokeTradeType.Dump))
                Context.User.SendPKMAsync(result, message).ConfigureAwait(false);
        }

        private void SendNotificationZ3(SeedSearchResult r)
        {
            var type = r.GetShinyType();
            var lines = r.ToString();

            var embed = new EmbedBuilder {Color = type == Shiny.AlwaysStar ? Color.Gold : Color.LighterGrey};
            embed.AddField(x =>
            {
                x.Name = "Seed Result";
                x.Value = lines;
                x.IsInline = false;
            });
            var msg = $"Here's your seed details for `{r.Seed:X16}`:";
            Context.User.SendMessageAsync(msg, embed: embed.Build()).ConfigureAwait(false);
        }
    }
}
