using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class LegalizerModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("convert"), Alias("showdown")]
    [Summary("Tries to convert the Showdown Set to pkm data.")]
    [Priority(1)]
    public Task ConvertShowdown([Summary("Generation/Format")] byte gen, [Remainder][Summary("Showdown Set")] string content)
    {
        return Context.Channel.ReplyWithLegalizedSetAsync(content, gen);
    }

    [Command("convert"), Alias("showdown")]
    [Summary("Tries to convert the Showdown Set to pkm data.")]
    [Priority(0)]
    public Task ConvertShowdown([Remainder][Summary("Showdown Set")] string content)
    {
        return Context.Channel.ReplyWithLegalizedSetAsync<T>(content);
    }

    [Command("legalize"), Alias("alm")]
    [Summary("Tries to legalize the attached pkm data.")]
    public async Task LegalizeAsync()
    {
        foreach (var att in (System.Collections.Generic.IReadOnlyCollection<global::Discord.Attachment>)Context.Message.Attachments)
            await Context.Channel.ReplyWithLegalizedSetAsync(att).ConfigureAwait(false);
    }
}
