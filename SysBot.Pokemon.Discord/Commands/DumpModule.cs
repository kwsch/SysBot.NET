using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Dump trades")]
    public class DumpModule : ModuleBase<SocketCommandContext>
    {
        internal static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        private const uint MaxTradeCode = 9999;

        [Command("dump")]
        [Alias("d")]
        [Summary("Dumps the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesDump))]
        public async Task DumpAsync(int code)
        {
            bool sudo = Context.User.GetIsSudo();
            await AddToQueueAsync(code, Context.User.Username, sudo).ConfigureAwait(false);
        }

        [Command("dump")]
        [Alias("d")]
        [Summary("Dumps the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesDump))]
        public async Task DumpAsync()
        {
            var code = Info.GetRandomTradeCode();
            await DumpAsync(code).ConfigureAwait(false);
        }

        [Command("dumpList")]
        [Alias("dl", "dq")]
        [Summary("Prints the users in the Dump queue.")]
        [RequireSudo]
        public async Task GetListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.Dump);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        private async Task AddToQueueAsync(int code, string trainer, bool sudo)
        {
            if ((uint)code > MaxTradeCode)
            {
                await ReplyAsync("Trade code should be 0000-9999!").ConfigureAwait(false);
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
            var result = AddToTradeQueue(new PK8(), code, trainer, sudo, PokeRoutineType.Dump, out var msg);
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
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Dump, code: code);
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