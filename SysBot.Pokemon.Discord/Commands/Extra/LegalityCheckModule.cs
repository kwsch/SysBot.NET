using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("legality", "Legality check commands.")]
[RequireContext(ContextType.Guild)]
public class LegalityCheckModule : SlashModuleBase
{
    [SlashCommand("legality", "Verifies an attached Pokémon file for legality.")]
    public Task LegalityCheck(
        [Summary(nameof(file), "Pokémon file to check.")] IAttachment file,
        [Summary(nameof(verbose), "Whether to provide a detailed report.")] bool? verbose = null)
        => CheckAsync(file, verbose ?? false);

    private async Task CheckAsync(IAttachment attachment, bool verbose)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var download = await attachment.DownloadEntityAsync().ConfigureAwait(false);
        if (!download.Success)
        {
            await FollowupAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var la = new LegalityAnalysis(download.Data!);
        var builder = new EmbedBuilder { Color = la.Valid ? Color.Green : Color.Red, Description = $"Legality Report for {download.SanitizedFileName}:" };
        builder.AddField(la.Valid ? "Valid" : "Invalid", la.Report(verbose));

        await FollowupAsync("Here's the legality report!", embed: builder.Build()).ConfigureAwait(false);
    }
}
