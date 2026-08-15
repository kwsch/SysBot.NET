using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("batch", "Batch editing commands.")]
[RequireContext(ContextType.Guild)]
public class BatchEditingModule : SlashModuleBase
{
    [SlashCommand("info", "Gets info about a requested property.")]
    public async Task GetBatchInfo(
        [Summary(nameof(propertyName), "The name of the property to get info about.")] string propertyName)
    {
        if (EntityBatchEditor.Instance.TryGetPropertyType(propertyName, out var result))
            await RespondAsync($"{propertyName}: {result}", ephemeral: true).ConfigureAwait(false);
        else
            await RespondAsync($"Unable to find info for {propertyName}.", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("validate", "Validates batch editor instructions.")]
    public async Task ValidateBatchInfo(
        [Summary(nameof(instructions), "The batch editor instructions to validate.")] string instructions)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var isValid = IsValidInstructionSet(instructions, out var invalid);
        if (isValid)
        {
            await FollowupAsync("All line(s) are valid.").ConfigureAwait(false);
            return;
        }

        var msg = invalid.Select(z => $"{z.PropertyName}, {z.PropertyValue}");
        var block = Format.Code(string.Join(Environment.NewLine, msg));
        await FollowupAsync($"Invalid Lines Detected:{block}").ConfigureAwait(false);
    }

    [SlashCommand("apply", "Applies batch editor instructions to the attachment.")]
    public async Task ApplyBatchInfo(
        [Summary(nameof(instructions), "The batch editor instructions to validate.")] string instructions,
        [Summary(nameof(attachment), "Attachment PKM file to edit.")] IAttachment attachment)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var isValid = IsValidInstructionSet(instructions, out var invalid);
        if (!isValid)
        {
            var msg = invalid.Select(z => $"{z.PropertyName}, {z.PropertyValue}");
            var block = Format.Code(string.Join(Environment.NewLine, msg));
            await FollowupAsync($"Invalid Lines Detected:{block}").ConfigureAwait(false);
            return;
        }

        var download = await attachment.DownloadEntityAsync().ConfigureAwait(false);
        if (!download.Success || download.Data is not { } pk)
        {
            await FollowupAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var set = new StringInstructionSet(instructions);
        var result = EntityBatchEditor.Instance.TryModify(pk, set.Filters, set.Instructions);
        if (result != ModifyResult.Modified)
        {
            await FollowupAsync($"Not modified: {result}").ConfigureAwait(false);
            return;
        }

        await Context.SendFileAsync(pk, "Modified result attached:").ConfigureAwait(false);
    }

    private static bool IsValidInstructionSet(ReadOnlySpan<char> split, out List<StringInstruction> invalid)
    {
        invalid = [];
        var set = new StringInstructionSet(split);
        foreach (var s in set.Filters.Concat(set.Instructions))
        {
            if (!EntityBatchEditor.Instance.TryGetPropertyType(s.PropertyName, out _))
                invalid.Add(s);
        }
        return invalid.Count == 0;
    }
}
