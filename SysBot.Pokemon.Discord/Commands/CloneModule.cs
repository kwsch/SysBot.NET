using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Clone trades")]
    public class CloneModule : ModuleBase<SocketCommandContext>
    {
        internal static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        private const uint MaxTradeCode = 9999;

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokemon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync(int code)
        {
            bool sudo = Context.User.GetIsSudo();
            await AddToQueueAsync(code, Context.User.Username, sudo).ConfigureAwait(false);
        }

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokemon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync()
        {
            var code = Info.GetRandomTradeCode();
            await CloneAsync(code).ConfigureAwait(false);
        }

        [Command("cloneList")]
        [Alias("cl", "cq")]
        [Summary("Prints the users in the Clone queue.")]
        [RequireSudo]
        public async Task GetListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.Clone);
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

            var result = AddToTradeQueue(new PK8(), code, trainer, sudo, PokeRoutineType.Clone, out var msg);
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
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Clone, code: code);
            var trade = new TradeEntry<PK8>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sudo);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = "Sorry, you are already in the queue.";
                return false;
            }

            msg = $"Added {user.Mention} to the queue. Your current position is: {Info.CheckPosition(userID, type).Position}";
            return true;
        }
    }
}
