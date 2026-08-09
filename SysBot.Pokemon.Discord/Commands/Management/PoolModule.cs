using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class PoolModule<T> : SlashModuleBase where T : PKM, new()
{

    [SlashCommand("pool", "Displays the details of Pokémon files in the random pool.")]
    public async Task DisplayPoolCountAsync()
    {
        var pool = SysCord<T>.Runner.Hub.Ledy.Pool;
        var count = pool.Count;
        if (count is <= 0 or >= 20)
        {
            await RespondAsync($"Pool Count: {count}", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Display the details of each Pokémon in the pool
        var entries = pool.Files.Select((z, i)
            => $"{i + 1:00}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");

        var msg = string.Join('\n', entries);
        var embed = new EmbedBuilder();
        embed.AddField($"Count: {count}", msg);
        await RespondAsync("Pool Details", ephemeral: true, embed: embed.Build()).ConfigureAwait(false);
    }
}
