using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Seed Check trades")]
    public class SeedCheckModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("seedCheck")]
        [Alias("checkMySeed", "checkSeed", "seed", "s", "sc")]
        [Summary("Checks the seed for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesSeed))]
        public async Task SeedCheckAsync(int code)
        {
            var sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(code, Context.User.Username, sudo, new PK8(), PokeRoutineType.SeedCheck, PokeTradeType.Seed).ConfigureAwait(false);
        }

        [Command("seedCheck")]
        [Alias("checkMySeed", "checkSeed", "seed", "s", "sc")]
        [Summary("Checks the seed for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesSeed))]
        public async Task SeedCheckAsync([Summary("Trade Code")][Remainder] string code)
        {
            int tradeCode = Util.ToInt32(code);
            bool sudo = Context.User.GetIsSudo();
            await Context.AddToQueueAsync(tradeCode == 0 ? Info.GetRandomTradeCode() : tradeCode, Context.User.Username, sudo, new PK8(), PokeRoutineType.SeedCheck, PokeTradeType.Seed).ConfigureAwait(false);
        }

        [Command("seedCheck")]
        [Alias("checkMySeed", "checkSeed", "seed", "s", "sc")]
        [Summary("Checks the seed for a Pokémon.")]
        [RequireQueueRole(nameof(DiscordManager.RolesSeed))]
        public async Task SeedCheckAsync()
        {
            var code = Info.GetRandomTradeCode();
            await SeedCheckAsync(code).ConfigureAwait(false);
        }

        [Command("seedList")]
        [Alias("sl", "scq", "seedCheckQueue", "seedQueue", "seedList")]
        [Summary("Prints the users in the Seed Check queue.")]
        [RequireSudo]
        public async Task GetSeedListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.SeedCheck);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("findFrame")]
        [Alias("ff", "getFrameData")]
        [Summary("Prints the next shiny frame from the provided seed.")]
        public async Task FindFrameAsync([Remainder]string seedString)
        {
            seedString = seedString.ToLower();
            if (seedString.StartsWith("0x"))
                seedString = seedString.Substring(2);

            var seed = Util.GetHexValue64(seedString);

            var r = new SeedSearchResult(Z3SearchResult.Success, seed, -1);
            var type = r.GetShinyType();
            var msg = r.ToString();

            var embed = new EmbedBuilder {Color = type == Shiny.AlwaysStar ? Color.Gold : Color.LighterGrey};

            embed.AddField(x =>
            {
                x.Name = "Seed Result";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync($"Here's your seed details for `{seed:X16}`:", embed: embed.Build()).ConfigureAwait(false);
        }
    }
}
