using Discord.Commands;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HelloModule : ModuleBase<SocketCommandContext>
{
    [Command("hello")]
    [Alias("hi", "hallo")]
    [Summary("Sagen Sie dem Bot \"Hallo\" und erhalten Sie eine Antwort.")]
    public async Task PingAsync()
    {
        var str = SysCordSettings.Settings.HelloResponse;
        var msg = string.Format(str, Context.User.Mention);
        await ReplyAsync(msg).ConfigureAwait(false);
    }
}
