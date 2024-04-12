using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Distribution Pool Module")]
public class PoolModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("poolReload")]
    [Summary("Lädt den Bot-Pool aus dem Einstellungsordner neu.")]
    [RequireSudo]
    public async Task ReloadPoolAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;

        var pool = hub.Ledy.Pool.Reload(hub.Config.Folder.DistributeFolder);
        if (!pool)
            await ReplyAsync("Das Nachladen aus dem Ordner ist fehlgeschlagen.").ConfigureAwait(false);
        else
            await ReplyAsync($"Neuladen aus dem Ordner. Anzahl der Pools: {hub.Ledy.Pool.Count}").ConfigureAwait(false);
    }

    [Command("pool")]
    [Summary("Zeigt die Details der Pokémon-Dateien im Zufalls-Pool an.")]
    public async Task DisplayPoolCountAsync()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var pool = hub.Ledy.Pool;
        var count = pool.Count;
        if (count is > 0 and < 20)
        {
            var lines = pool.Files.Select((z, i) => $"{i + 1:00}: {z.Key} = {(Species)z.Value.RequestInfo.Species}");
            var msg = string.Join("\n", lines);

            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = $"Anzahl: {count}";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("Pool-Details", embed: embed.Build()).ConfigureAwait(false);
        }
        else
        {
            await ReplyAsync($"Anzahl der Pools: {count}").ConfigureAwait(false);
        }
    }
}
