using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        private static readonly HashSet<ulong> UsersInQueue = new HashSet<ulong>();

        [Command("trade")]
        [Summary("Makes the bot trade you the provided PKM by adding it to the pool.")]
        public async Task TradeAsync([Summary("Trade Code")]int code, [Remainder][Summary("Trainer Name to trade to.")]string trainerName)
        {
            var cfg = SysCordInstance.Self.Hub.Config;
            var sudo = GetHasRole(cfg.DiscordRoleCanTrade);
            var allowed = sudo || GetHasRole(cfg.DiscordRoleCanTrade);

            if (!allowed)
            {
                await ReplyAsync("Sorry, you are not permitted to use this command!").ConfigureAwait(false);
                return;
            }

            if ((uint)code > 9999)
            {
                await ReplyAsync("Trade code should be 0000-9999!").ConfigureAwait(false);
                return;
            }

            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
            if (!att.Success || !(att.Data is PK8 pk8))
            {
                await ReplyAsync("No PK8 attachment provided!").ConfigureAwait(false);
                return;
            }

            var la = new LegalityAnalysis(pk8);
            if (!la.Valid)
            {
                await ReplyAsync("PK8 attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }

            var userID = Context.User.Id;
            if (UsersInQueue.Contains(userID) && !sudo)
            {
                await ReplyAsync("Sorry, you are already in the queue.").ConfigureAwait(false);
                return;
            }
            UsersInQueue.Add(userID);

            var tmp = new PokeTradeTrainerInfo(trainerName);
            var notifier = new DiscordTradeNotifier<PK8>(pk8, tmp, code, Context);
            var detail = new PokeTradeDetail<PK8>(pk8, tmp, notifier, code: code);
            var priority = sudo ? PokeTradeQueue<PK8>.Tier1 : PokeTradeQueue<PK8>.TierFree;
            SysCordInstance.Self.Hub.Queue.Enqueue(detail, priority);

            await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);

            await ReplyAsync($"Added ${Context.User.Mention} to the queue. Your current position is: {SysCordInstance.Self.Hub.Queue.Count}")
                .ConfigureAwait(false);

            notifier.OnFinish = () => UsersInQueue.Remove(userID);
        }

        [Command("trade")]
        public async Task TradeAsync()
        {
            await TradeAsync(Util.Rand.Next(0, 9999), string.Empty).ConfigureAwait(false);
        }

        private bool GetHasRole(string RequiredRole)
        {
            var guild = Context.Guild;
            var role = guild.Roles.First(x => x.Name == RequiredRole);

            var igu = (SocketGuildUser) Context.User;
            bool hasRole = igu.Roles.Contains(role);
            return hasRole;
        }

        private class DiscordTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM
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
                Context.User.SendMessageAsync($"Initializing trade ({Data.Nickname}). Please be ready. Your code is {Code:0000}.").ConfigureAwait(false);
            }

            public void TradeSearching(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
            {
                var name = Info.TrainerName;
                var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", ({name})";
                Context.User.SendMessageAsync($"I'm searching for you, {trainer}! Your code is {Code:0000}.").ConfigureAwait(false);
            }

            public void TradeCanceled(PokeRoutineExecutor routine, PokeTradeDetail<T> info, PokeTradeResult msg)
            {
                Context.User.SendMessageAsync($"Trade has been canceled: {msg}").ConfigureAwait(false);
                OnFinish!();
            }

            public void TradeFinished(PokeRoutineExecutor routine, PokeTradeDetail<T> info)
            {
                Context.User.SendMessageAsync($"Trade has been finished. Enjoy your {(Species)Data.Species}!").ConfigureAwait(false);
                OnFinish!();
            }
        }
    }
}