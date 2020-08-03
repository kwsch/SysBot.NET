using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.AnimalCrossing
{
    public class ControlModule : ModuleBase<SocketCommandContext>
    {
        [Command("detatch")]
        [Summary("Detatches the virtual controller so the operator can use their own handheld controller temporarily.")]
        [RequireSudo]
        public async Task DetatchAsync()
        {
            Globals.Bot.CleanRequested = true;
            await ReplyAsync("A clean request will be executed momentarily.").ConfigureAwait(false);
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
