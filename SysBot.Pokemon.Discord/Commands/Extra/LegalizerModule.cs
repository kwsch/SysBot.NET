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
        await DeferAsync().ConfigureAwait(false);
        await Context.ReplyWithLegalizedSetAsync(file).ConfigureAwait(false);
    }

    [SlashCommand("convert", "Converts a Showdown Set to PKM data.")]
    public async Task ConvertShowdownAsync(
        [Summary(nameof(content), "The Showdown set to convert.")] string content,
        [Summary(nameof(generation), "Optional")] byte? generation = null)
    {
        await DeferAsync().ConfigureAwait(false);
        if (generation is not { } gen) // assume current format if no generation is specified
            await Context.ReplyWithLegalizedSetAsync<T>(content).ConfigureAwait(false);
        else
            await Context.ReplyWithLegalizedSetAsync(content, gen).ConfigureAwait(false);
    }
}
