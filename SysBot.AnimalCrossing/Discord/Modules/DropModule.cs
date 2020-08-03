using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.AnimalCrossing
{
    public class DropModule : ModuleBase<SocketCommandContext>
    {
        private const int MaxRequestCount = 7;

        [Command("clean")]
        [Summary("Picks up items around the bot.")]
        public async Task RequestCleanAsync()
        {
            if (!Globals.Bot.Config.AllowClean)
            {
                await ReplyAsync("Clean functionality is currently disabled.").ConfigureAwait(false);
                return;
            }
            Globals.Bot.CleanRequested = true;
            await ReplyAsync("A clean request will be executed momentarily.").ConfigureAwait(false);
        }

        [Command("code")]
        [Alias("dodo")]
        [Summary("Prints the Dodo Code for the island.")]
        public async Task RequestDodoCodeAsync()
        {
            await ReplyAsync($"Dodo Code: {Globals.Bot.DodoCode}.").ConfigureAwait(false);
        }

        [Command("dropItem")]
        [Alias("drop")]
        [Summary("Drops a custom item (or items).")]
        public async Task RequestDropAsync([Remainder]string request)
        {
            var split = request.Split(new[] { " ", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var items = DropUtil.GetItems(split, Globals.Bot.Config);
            await DropItems(items).ConfigureAwait(false);
        }

        [Command("dropDIY")]
        [Alias("diy")]
        [Summary("Drops a DIY recipe with the requested recipe ID(s).")]
        public async Task RequestDropDIYAsync([Remainder]string recipeIDs)
        {
            var split = recipeIDs.Split(new[] { " ", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var items = DropUtil.GetDIYItems(split);
            await DropItems(items).ConfigureAwait(false);
        }

        private async Task DropItems(IReadOnlyCollection<Item> items)
        {
            const int maxRequestCount = 7;
            if (items.Count > maxRequestCount)
            {
                var clamped = $"Users are limited to {MaxRequestCount} items per command. Please use this bot responsibly.";
                await ReplyAsync(clamped).ConfigureAwait(false);
                items = items.Take(MaxRequestCount).ToArray();
            }

            var requestInfo = new ItemRequest(Context.User.Username, items);
            Globals.Bot.Injections.Enqueue(requestInfo);

            var msg = $"Item drop request{(requestInfo.Items.Count > 1 ? "s" : string.Empty)} will be executed momentarily.";
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }
}
