using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class LegalizerModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("legalize"), Alias("alm")]
    [Summary("Versucht, die angehängten pkm-Daten zu legalisieren.")]
    public async Task LegalizeAsync()
    {
        var attachments = Context.Message.Attachments;
        foreach (var att in attachments)
            await Context.Channel.ReplyWithLegalizedSetAsync(att).ConfigureAwait(false);
    }

    [Command("convert"), Alias("showdown")]
    [Summary("Versucht, das Showdown-Set in pkm-Daten zu konvertieren.")]
    [Priority(1)]
    public Task ConvertShowdown([Summary("Generation/Format")] byte gen, [Remainder][Summary("Showdown Set")] string content)
    {
        return Context.Channel.ReplyWithLegalizedSetAsync(content, gen);
    }

    [Command("convert"), Alias("showdown")]
    [Summary("Versucht, das Showdown-Set in pkm-Daten zu konvertieren.")]
    [Priority(0)]
    public Task ConvertShowdown([Remainder][Summary("Showdown Set")] string content)
    {
        return Context.Channel.ReplyWithLegalizedSetAsync<T>(content);
    }
}
