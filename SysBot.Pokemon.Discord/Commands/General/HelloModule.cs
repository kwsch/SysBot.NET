using System.Threading.Tasks;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class HelloModule : SlashModuleBase
{
    [SlashCommand("hello", "Say hello to the bot and get a response.")]
    public Task HelloAsync() => RespondAsync(string.Format(SysCordSettings.Settings.HelloResponse, Context.User.Mention));

    [SlashCommand("ping", "Makes the bot respond, indicating that it is running.")]
    public Task PingAsync() => RespondAsync("Pong!", ephemeral: true);
}
