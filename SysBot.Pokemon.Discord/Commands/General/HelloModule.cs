using Discord.Commands;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HelloModule : ModuleBase<SocketCommandContext>
{
    [Command("hello")]
    [Alias("hi")]
    [Summary("Say hello to the bot and get a response.")]
    public async Task PingAsync()
    {
        var str = SysCordSettings.Settings.HelloResponse;
        var msg = string.Format(str, Context.User.Mention);
        await ReplyAsync(msg).ConfigureAwait(false);
    }
}
