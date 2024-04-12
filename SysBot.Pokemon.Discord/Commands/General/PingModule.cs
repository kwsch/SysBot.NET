using Discord.Commands;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class PingModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Bringt den Bot zum Reagieren und zeigt damit an, dass er läuft.")]
    public async Task PingAsync()
    {
        await ReplyAsync("Pong!").ConfigureAwait(false);
    }
}
