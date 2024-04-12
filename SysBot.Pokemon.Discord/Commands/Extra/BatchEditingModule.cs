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
    [Summary("Es wird versucht, Informationen über die angeforderte Eigenschaft zu erhalten.")]
    public async Task GetBatchInfo(string propertyName)
    {
        var result = BatchEditing.GetPropertyType(propertyName);
        if (string.IsNullOrWhiteSpace(result))
            await ReplyAsync($"Unable to find info for {propertyName}.").ConfigureAwait(false);
        else
            await ReplyAsync($"{propertyName}: {result}").ConfigureAwait(false);
    }

    [Command("batchValidate"), Alias("bev")]
    [Summary("Es wird versucht, Informationen über die angeforderte Eigenschaft zu erhalten.")]
    public async Task ValidateBatchInfo(string instructions)
    {
        bool valid = IsValidInstructionSet(instructions, out var invalid);

        if (!valid)
        {
            var msg = invalid.Select(z => $"{z.PropertyName}, {z.PropertyValue}");
            await ReplyAsync($"Ungültige Zeilen entdeckt:\r\n{Format.Code(string.Join(Environment.NewLine, msg))}")
                .ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync($"{invalid.Count} Zeile(n) sind ungültig.").ConfigureAwait(false);
        }
    }

    private static bool IsValidInstructionSet(ReadOnlySpan<char> split, out List<StringInstruction> invalid)
    {
        invalid = [];
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
