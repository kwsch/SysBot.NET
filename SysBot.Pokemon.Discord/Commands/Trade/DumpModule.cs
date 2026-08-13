using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class DumpModule<T> : SlashModuleBase where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("dump", "Dumps the Pokémon you show via Link Trade.")]
    [RequireQueueRole(PokeRoutineType.Dump)]
    [RequireOpenDms]
    public async Task DumpAsync(
        [Summary(nameof(code), "Optional; leave blank for a random code")] int? code = null)
    {
        if (!await Context.IsTradeCodeValidOrEmpty(code).ConfigureAwait(false))
            return;

        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        code ??= Info.GetRandomTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, (int)code, new T(), PokeRoutineType.Dump, PokeTradeType.Dump).ConfigureAwait(false);
    }

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("dump-list", "Prints the users in the Dump queue.")]
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Dump);
        var embed = new EmbedBuilder { Color = Color.LightGrey };
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });

        await RespondAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }
}
