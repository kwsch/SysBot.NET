using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using SysBot.Base;

namespace SysBot.AnimalCrossing
{
    public class ControlModule : ModuleBase<SocketCommandContext>
    {
        [Command("detatch")]
        [Summary("Detatches the virtual controller so the operator can use their own handheld controller temporarily.")]
        [RequireSudo]
        public async Task DetatchAsync()
        {
            await ReplyAsync("A controller detatch request will be executed momentarily.").ConfigureAwait(false);
            var bot = Globals.Bot;
            await bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None).ConfigureAwait(false);
        }

        [Command("setCode")]
        [Summary("Sets a string to the Dodo Code property for users to call via the associated command.")]
        [RequireSudo]
        public async Task SetDodoCodeAsync([Remainder]string code)
        {
            var bot = Globals.Bot;
            bot.DodoCode = code;
            await ReplyAsync($"The dodo code for the bot has been set to {code}.").ConfigureAwait(false);
        }

        [Command("toggleRequests")]
        [Summary("Toggles accepting drop requests.")]
        [RequireSudo]
        public async Task ToggleRequestsAsync()
        {
            bool value = (Globals.Bot.Config.AcceptingCommands ^= true);
            await ReplyAsync($"Accepting drop requests: {value}.").ConfigureAwait(false);
        }
    }
}
