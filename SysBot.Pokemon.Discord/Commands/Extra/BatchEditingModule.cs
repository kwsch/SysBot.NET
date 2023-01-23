using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord
{
    // ReSharper disable once UnusedType.Global
    public class BatchEditingModule : ModuleBase<SocketCommandContext>
    {
        [Command("batchInfo"), Alias("bei")]
        [Summary("Tries to get info about the requested property.")]
        public async Task GetBatchInfo(string propertyName)
        {
            var result = BatchEditing.GetPropertyType(propertyName);
            if (string.IsNullOrWhiteSpace(result))
                await ReplyAsync($"Unable to find info for {propertyName}.").ConfigureAwait(false);
            else
                await ReplyAsync($"{propertyName}: {result}").ConfigureAwait(false);
        }

        [Command("batchValidate"), Alias("bev")]
        [Summary("Tries to get info about the requested property.")]
        public async Task ValidateBatchInfo(string instructions)
        {
            bool valid = IsValidInstructionSet(instructions, out var invalid);

            if (!valid)
            {
                var msg = invalid.Select(z => $"{z.PropertyName}, {z.PropertyValue}");
                await ReplyAsync($"Invalid Lines Detected:\r\n{Format.Code(string.Join(Environment.NewLine, msg))}")
                    .ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync($"{invalid.Count} line(s) are invalid.").ConfigureAwait(false);
            }
        }

        private static bool IsValidInstructionSet(ReadOnlySpan<char> split, out List<StringInstruction> invalid)
        {
            invalid = new List<StringInstruction>();
            var set = new StringInstructionSet(split);
            foreach (var s in set.Filters.Concat(set.Instructions))
            {
                var type = BatchEditing.GetPropertyType(s.PropertyName);
                if (type == null)
                    invalid.Add(s);
            }

            return invalid.Count == 0;
        }
    }
}
