using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Link Code trades")]
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        internal static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        private const uint MaxTradeCode = 9999;

        [Command("tradeList")]
        [Alias("tl")]
        [Summary("Prints the users in the trade queues.")]
        [RequireSudo]
        public async Task GetTradeListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided PKM by adding it to the pool.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Trade Code")]int code, [Remainder][Summary("Trainer Name to trade to.")]string trainerName)
        {
            var sudo = Context.User.GetIsSudo();

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

            await AddTradeToQueueAsync(code, trainerName, pk8, sudo).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided Showdown Set by adding it to the pool.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder]string content)
        {
            const int gen = 8;
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var sav = AutoLegalityWrapper.GetTrainerInfo(gen);

            var pkm = sav.GetLegal(set, out var result);
            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[set.Species];
            var msg = la.Valid
                ? $"Here's your ({result}) legalized PKM for {spec} ({la.EncounterOriginal.Name})!"
                : $"Oops! I wasn't able to create something from that. Here's my best attempt for that {spec}!";

            if ((!la.Valid && SysCordInstance.Self.Hub.Config.VerifyLegality) || !(pkm is PK8 pk8))
            {
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            pk8.ResetPartyStats();

            var code = Info.GetRandomTradeCode();
            var sudo = Context.User.GetIsSudo();
            await AddTradeToQueueAsync(code, Context.User.Username, pk8, sudo).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the attached file by adding it to the pool.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync()
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsync(code, Context.User.Username).ConfigureAwait(false);
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, PK8 pk8, bool sudo)
        {
            if ((uint)code > MaxTradeCode)
            {
                await ReplyAsync("Trade code should be 0000-9999!").ConfigureAwait(false);
                return;
            }

            var la = new LegalityAnalysis(pk8);
            if (!la.Valid && SysCordInstance.Self.Hub.Config.VerifyLegality)
            {
                await ReplyAsync("PK8 attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }

            try
            {
                await Context.User.SendMessageAsync("I've added you to the queue! I'll message you here when your trade is starting.").ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                await ReplyAsync($"{ex.HttpCode}: {ex.Reason}!").ConfigureAwait(false);
                await ReplyAsync("You must enable private messages in order to be queued!").ConfigureAwait(false);
                return;
            }
            var result = AddToTradeQueue(pk8, code, trainerName, sudo, PokeRoutineType.LinkTrade, out var msg);
            await ReplyAsync(msg).ConfigureAwait(false);
            if (result)
                await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
        }

        private bool AddToTradeQueue(PK8 pk8, int code, string trainerName, bool sudo, PokeRoutineType type, out string msg)
        {
            var user = Context.User;
            var userID = user.Id;
            var name = user.Username;

            var trainer = new PokeTradeTrainerInfo(trainerName);
            var notifier = new DiscordTradeNotifier<PK8>(pk8, trainer, code, Context);
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Specific, code: code);
            var trade = new TradeEntry<PK8>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sudo);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = "Sorry, you are already in the queue.";
                return false;
            }

            msg = $"Added {user.Mention} to the queue for trade type: {type}. Your current position is: {Info.CheckPosition(userID, type).Position}";
            return true;
        }
    }
}
