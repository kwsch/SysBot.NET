using Discord.Commands;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class EchoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("toss")]
    [Summary("Makes all bots that are currently waiting for a go-ahead continue operation.")]
    [RequireSudo]
    public async Task TossAsync(string name = "")
    {
        var bots = SysCord<T>.Runner.Bots.Select(z => z.Bot);
        foreach (var b in bots)
        {
            if (b is not IEncounterBot x)
                continue;
            if (!b.Connection.Name.Contains(name) && !b.Connection.Label.Contains(name))
                continue;
            x.Acknowledge();
        }

        await ReplyAsync("Done.").ConfigureAwait(false);
    }
}
