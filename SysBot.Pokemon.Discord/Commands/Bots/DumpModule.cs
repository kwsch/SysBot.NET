using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Dump trades")]
    public class DumpModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("dump")]
        [Alias("d")]
        [Summary("Dumps the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesDump))]
        public async Task DumpAsync(int code)
        {
            bool sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(code, Context.User.Username, sudo, new PK8(), PokeRoutineType.Dump, PokeTradeType.Dump).ConfigureAwait(false);
        }

        [Command("dump")]
        [Alias("d")]
        [Summary("Dumps the Pokémon you show via Link Trade.")]
        [RequireQueueRole(nameof(DiscordManager.RolesDump))]
        public async Task DumpAsync([Summary("Trade Code")][Remainder] string code)
        {
            int tradeCode = Util.ToInt32(code);
            bool sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(tradeCode == 0 ? Info.GetRandomTradeCode() : tradeCode, Context.User.Username, sudo, new PK8(), PokeRoutineType.Dump, PokeTradeType.Dump).ConfigureAwait(false);
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
    }
}