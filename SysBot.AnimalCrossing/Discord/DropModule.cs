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
            var split = request.Split(' ');
            var items = GetItems(split, Globals.Bot.Config);

            var requestInfo = new ItemRequest(Context.User.Username, items);

            Globals.Bot.Injections.Enqueue(requestInfo);
            await ReplyAsync($"Item drop request{(requestInfo.Items.Count > 1 ? "s" : string.Empty)} will be executed momentarily.").ConfigureAwait(false);
        }

        private static IReadOnlyCollection<byte[]> GetItems(IReadOnlyList<string> split, CrossBotConfig botConfig)
        {
            var result = new byte[split.Count][];
            for (int i = 0; i < result.Length; i++)
            {
                var text = split[i];
                var convert = GetBytesFromString(text);
                var bytes = GetItemBytes(convert, i, botConfig);
                result[i] = bytes;
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

        private static byte[] GetItemBytes(byte[] convert, int i, IConfigItem config)
        {
            byte[] bytes;
            Item item;
            try
            {
                item = convert.ToClass<Item>();
                bytes = item.ToBytesClass();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to convert item {i}: {ex.Message}");
            }

            if (!IsSaneItem(item) || bytes.Length != Item.SIZE)
                throw new Exception($"Unsupported item: {i}");

            if (config.WrapAllItems)
            {
                item.SetWrapping(ItemWrapping.WrappingPaper, config.WrappingPaper, true);
                bytes = item.ToBytesClass();
            }
            return bytes;
        }

        private static bool IsSaneItem(Item item)
        {
            if (item.IsFieldItem)
                return false;
            if (item.IsExtension)
                return false;
            if (item.IsNone)
                return false;
            return true;
        }
    }
}
