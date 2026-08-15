using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class LegalizerModule<T> : SlashModuleBase where T : PKM, new()
{
    [SlashCommand("legalize", "Tries to legalize an attached PKM file.")]
    public async Task LegalizeAsync(
        [Summary(nameof(file), "The file to legalize.")] IAttachment file)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        await Context.ReplyWithLegalizedSetAsync(file).ConfigureAwait(false);
    }

    [SlashCommand("transfer", "Transfers a PKM to another format.")]
    public async Task TransferAsync(
        [Summary(nameof(file), "The file to legalize.")] IAttachment file,
        [Summary(nameof(type), "The target format type.")] string type)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var download = await file.DownloadEntityAsync().ConfigureAwait(false);
        if (!download.Success || download.Data is not { } pk)
        {
            await FollowupAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var blank = EntityBlank.GetBlank(type).GetType();
        var converted = EntityConverter.ConvertToType(pk, blank, out var result);
        if (converted is null)
            await FollowupAsync($"Failed to convert your attachment to {type}: {result}").ConfigureAwait(false);
        else
            await Context.SendFileAsync(converted, $"Successfully converted your attached file to {type}.").ConfigureAwait(false);
    }

    [SlashCommand("convert", "Converts a Showdown Set to PKM data.")]
    public async Task ConvertAsync(
        [Summary(nameof(content), "The Showdown set to convert.")] string content,
        [Summary(nameof(version), "Optional: Original Trainer version to obtain the encounter with.")] GameVersion? version = null)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        if (version is null) // assume current format if no version is specified
            await Context.ReplyWithLegalizedSetAsync<T>(content).ConfigureAwait(false);
        else
            await Context.ReplyWithLegalizedSetAsync(content, version.Value).ConfigureAwait(false);
    }
}
