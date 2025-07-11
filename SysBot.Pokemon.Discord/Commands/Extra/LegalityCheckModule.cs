using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class LegalityCheckModule : ModuleBase<SocketCommandContext>
{
    [Command("lc"), Alias("check", "validate", "verify")]
    [Summary("Verifies the attachment for legality.")]
    public async Task LegalityCheck()
    {
        var attachments = Context.Message.Attachments;
        foreach (var att in attachments)
            await LegalityCheck(att, false).ConfigureAwait(false);
    }

    [Command("lcv"), Alias("verbose")]
    [Summary("Verifies the attachment for legality with a verbose output.")]
    public async Task LegalityCheckVerbose()
    {
        var attachments = Context.Message.Attachments;
        foreach (var att in attachments)
            await LegalityCheck(att, true).ConfigureAwait(false);
    }

    private async Task LegalityCheck(IAttachment att, bool verbose)
    {
        var download = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!download.Success)
        {
            await ReplyAsync(download.ErrorMessage).ConfigureAwait(false);
            return;
        }

        var pkm = download.Data!;
        var la = new LegalityAnalysis(pkm);
        var builder = new EmbedBuilder
        {
            Color = la.Valid ? Color.Green : Color.Red,
            Description = $"Legality Report for {download.SanitizedFileName}:",
        };

        builder.AddField(x =>
        {
            x.Name = la.Valid ? "Valid" : "Invalid";
            x.Value = la.Report(verbose);
            x.IsInline = false;
        });

        await ReplyAsync("Here's the legality report!", false, builder.Build()).ConfigureAwait(false);
    }
}
