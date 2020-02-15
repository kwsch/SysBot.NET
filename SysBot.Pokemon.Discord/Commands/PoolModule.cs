using System.Threading.Tasks;
using Discord.Commands;

namespace SysBot.Pokemon.Discord
{
    public class PoolModule : ModuleBase<SocketCommandContext>
    {
        [Command("poolReload")]
        [Summary("Reloads the bot pool from the setting's folder.")]
        public async Task ReloadPoolAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;
            if (!Context.GetIsSudo())
            {
                await ReplyAsync("You are not permitted to use this command.").ConfigureAwait(false);
                return;
            }

            var pool = hub.Pool.LoadFolder(hub.Config.DistributeFolder);
            if (!pool)
                await ReplyAsync($"Failed to reload from folder.").ConfigureAwait(false);
            else
                await ReplyAsync($"Reloaded from folder. Pool count: {hub.Pool.Count}").ConfigureAwait(false);
        }

        [Command("poolCount")]
        [Summary("Displays the count of Pokémon files in the random pool.")]
        public async Task DisplayPoolCountAsync()
        {
            var me = SysCordInstance.Self;
            var hub = me.Hub;
            await ReplyAsync($"Pool count: {hub.Pool.Count}").ConfigureAwait(false);
        }
    }
}