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
    public async Task DumpAsync(
        [Summary(nameof(code), "Optional; leave blank for a random code")] int? code = null)
    {
        if (!await RequireAsync(CheckQueueAccess(PokeRoutineType.Dump, out var error), error).ConfigureAwait(false))
            return;

        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var sig = GetSignificance(Context.User);
        code ??= Info.GetRandomTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, (int)code, sig, new T(), PokeRoutineType.Dump, PokeTradeType.Dump, Context).ConfigureAwait(false);
    }

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("dump-list", "Prints the users in the Dump queue.")]
    [RequireUserPermission(ChannelPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    public async Task GetListAsync()
    {
        if (!await RequireAsync(CheckSudo(out var error), error).ConfigureAwait(false))
            return;

        string msg = Info.GetTradeList(PokeRoutineType.Dump);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });

        await RespondAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }
}
