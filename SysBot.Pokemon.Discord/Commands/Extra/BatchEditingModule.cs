using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

// ReSharper disable once UnusedType.Global
public class BatchEditingModule : ModuleBase<SocketCommandContext>
{
    [Command("batchInfo"), Alias("bei")]
    [Summary("Tries to get info about the requested property.")]
    public async Task GetBatchInfo(string propertyName)
    {
        if (BatchEditing.TryGetPropertyType(propertyName, out var result))
            await ReplyAsync($"{propertyName}: {result}").ConfigureAwait(false);
        else
            await ReplyAsync($"Unable to find info for {propertyName}.").ConfigureAwait(false);
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
        invalid = [];
        var set = new StringInstructionSet(split);
        foreach (var s in set.Filters.Concat(set.Instructions))
        {
            if (!BatchEditing.TryGetPropertyType(s.PropertyName, out _))
                invalid.Add(s);
        }

        return invalid.Count == 0;
    }
}
