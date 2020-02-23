using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Dudu trades")]
    public class DuduModule : ModuleBase<SocketCommandContext>
    {
        internal static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        private const uint MaxTradeCode = 9999;

        [Command("seedCheck")]
        [Alias("dudu", "d", "sc")]
        [Summary("Checks the seed for a pokemon.")]
        public async Task SeedCheckAsync(int code)
        {
            var cfg = Info.Hub.Config;
            var sudo = Context.GetHasRole(cfg.DiscordRoleSudo);
            var allowed = sudo || (Context.GetHasRole(cfg.DiscordRoleCanDudu) && Info.CanQueue);
            if (!allowed)
            {
                await ReplyAsync("Sorry, you are not permitted to use this command!").ConfigureAwait(false);
                return;
            }

            if ((uint)code > MaxTradeCode)
            {
                await ReplyAsync("Trade code should be 0000-9999!").ConfigureAwait(false);
                return;
            }
            await AddSeedCheckToQueueAsync(code, Context.User.Username, sudo).ConfigureAwait(false);
        }

        [Command("seedCheck")]
        [Alias("dudu", "d", "sc")]
        [Summary("Checks the seed for a pokemon.")]
        public async Task SeedCheckAsync()
        {
            var code = Info.GetRandomTradeCode();
            await SeedCheckAsync(code).ConfigureAwait(false);
        }

        [Command("duduList")]
        [Alias("dl", "scq", "seedCheckQueue", "duduQueue", "seedList")]
        [Summary("Prints the users in the Seed Check queue.")]
        public async Task GetSeedListAsync()
        {
            if (!Context.GetIsSudo(Info.Hub.Config))
            {
                await ReplyAsync("You can't use this command.").ConfigureAwait(false);
                return;
            }

            string msg = Info.GetTradeList(PokeRoutineType.DuduBot);
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

            var r = new Z3SeedResult(Z3SearchResult.Success, seed, -1);
            var msg = r.ToString();
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Seed Result";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync($"Here's your seed details for `{seed:X16}`:", embed: embed.Build()).ConfigureAwait(false);
        }

        private async Task AddSeedCheckToQueueAsync(int code, string trainer, bool sudo)
        {
            var result = AddToTradeQueue(new PK8(), code, trainer, sudo, PokeRoutineType.DuduBot, out var msg);
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
            var detail = new PokeTradeDetail<PK8>(pk8, trainer, notifier, PokeTradeType.Dudu, code: code);
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