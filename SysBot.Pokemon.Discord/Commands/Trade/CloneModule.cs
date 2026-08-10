using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class CloneModule<T> : SlashModuleBase where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("clone", "Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(PokeRoutineType.Clone)]
    public Task CloneAsync(int? code = null) => JoinAsync(code);
    private async Task JoinAsync(int? code)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var sig = GetSignificance(Context.User);
        code ??= Info.GetRandomTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, (int)code, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone, Context).ConfigureAwait(false);
    }

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("clone-list", "Prints the users in the Clone queue.")]
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [RequireSudo]
    public async Task GetListAsync()
    {
        var embed = new EmbedBuilder();
        embed.AddField("Pending Trades", Info.GetTradeList(PokeRoutineType.Clone));
        await RespondAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

}
