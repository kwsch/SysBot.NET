using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[Group("encounter", "Control over an encounter bot.")]
public class EncounterModule<T> : SudoModuleBase where T : PKM, new()
{
    [SlashCommand("toss", "Makes waiting bots continue operation.")]
    public async Task TossAsync(
        [Summary(nameof(name), "Bot label to match. Leave blank to toss for all.")] string name = "")
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        foreach (var b in SysCord<T>.Runner.Bots.Select(z => z.Bot))
        {
            if (!b.Connection.Name.Contains(name) && !b.Connection.Label.Contains(name))
                continue;
            if (b is IEncounterBot enc)
                enc.Acknowledge();
        }

        await RespondAsync("Done.").ConfigureAwait(false);
    }
}
