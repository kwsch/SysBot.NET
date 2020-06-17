using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Clone trades")]
    public class CloneModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync(int code)
        {
            bool sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(code, Context.User.Username, sudo, new PK8(), PokeRoutineType.Clone, PokeTradeType.Clone).ConfigureAwait(false);
        }

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesClone))]
        public async Task CloneAsync([Summary("Trade Code")][Remainder] string code)
        {
            int tradeCode = Util.ToInt32(code);
            bool sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(tradeCode == 0 ? Info.GetRandomTradeCode() : tradeCode, Context.User.Username, sudo, new PK8(), PokeRoutineType.Clone, PokeTradeType.Clone).ConfigureAwait(false);
        }

        [Command("clone")]
        [Alias("c")]
        [Summary("Clones the Pokémon you show via Link Trade.")]
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
    }
}
