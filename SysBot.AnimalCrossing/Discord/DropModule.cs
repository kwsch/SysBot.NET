using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.AnimalCrossing
{
    public class DropModule : ModuleBase<SocketCommandContext>
    {
        [Command("drop")]
        [Summary("Drops a custom item (or items).")]
        public async Task RequestDropAsync([Remainder]string request)
        {
            var split = request.Split(new[] {" ", "\n", "\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var items = GetItems(split, Globals.Bot.Config);

            var requestInfo = new ItemRequest(Context.User.Username, items);

            Globals.Bot.Injections.Enqueue(requestInfo);
            await ReplyAsync($"Item drop request{(requestInfo.Items.Count > 1 ? "s" : string.Empty)} will be executed momentarily.").ConfigureAwait(false);
        }

        private static IReadOnlyCollection<Item> GetItems(IReadOnlyList<string> split, IConfigItem config)
        {
            var result = new Item[split.Count];
            for (int i = 0; i < result.Length; i++)
            {
                var text = split[i];
                var convert = GetBytesFromString(text);
                result[i] = CreateItem(convert, i, config);
            }
            return result;
        }

        private static byte[] GetBytesFromString(string text)
        {
            return Enumerable.Range(0, text.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(text.Substring(x, 2), 16))
                .Reverse().ToArray();
        }

        private static Item CreateItem(byte[] convert, int i, IConfigItem config)
        {
            Item item;
            try
            {
                item = convert.ToClass<Item>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert item {i}: {ex.Message}");
            }

            if (!IsSaneItem(item) || convert.Length != Item.SIZE)
                throw new Exception($"Unsupported item: {i}");

            if (config.WrapAllItems && item.ShouldWrapItem())
                item.SetWrapping(ItemWrapping.WrappingPaper, config.WrappingPaper, true);
            return item;
        }

        private static bool IsSaneItem(Item item)
        {
            if (item.IsFieldItem)
                return false;
            if (item.IsExtension)
                return false;
            if (item.IsNone)
                return false;
            if (item.SystemParam > 3)
                return false; // buried, dropped, etc

            if (item.ItemId == Item.MessageBottle || item.ItemId == Item.MessageBottleEgg)
            {
                item.ItemId = Item.DIYRecipe;
                item.FreeParam = 0;
            }

            return true;
        }
    }
}
