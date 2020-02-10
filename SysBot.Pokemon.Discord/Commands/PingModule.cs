using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.Pokemon.Discord
{
    public class PingModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Makes the bot respond, indicating that it is running.")]
        public async Task PingAsync()
        {
            await ReplyAsync("Pong!").ConfigureAwait(false);
        }
    }
}