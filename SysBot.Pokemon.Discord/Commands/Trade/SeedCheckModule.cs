using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class SeedCheckModule<T> : SlashModuleBase where T : PKM, new()
{
    private static TradeQueueInfo<T> Info=>SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("seed-check", "Checks the seed for a Pokémon.")]
    public async Task SeedCheckAsync(
        [Summary(nameof(code), "Optional; leave blank for a random code")] int? code = null)
    {
        if (!await RequireAsync(CheckQueueAccess(PokeRoutineType.SeedCheck, out var e), e).ConfigureAwait(false))
            return;

        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var sig = GetSignificance(Context.User);
        code ??= Info.GetRandomTradeCode();
        await QueueHelper<T>.AddToQueueAsync(Context, (int)code, sig, new T(), PokeRoutineType.SeedCheck, PokeTradeType.Seed, Context).ConfigureAwait(false);
    }

    [SlashCommand("find-frame", "Prints the next shiny frame from a seed.")]
    public async Task FindFrameAsync(
        [Summary(nameof(seed), "The seed to find the next shiny frame from.")] string seed)
    {
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var s = seed.ToLowerInvariant().AsSpan();
        if (s.StartsWith("0x"))
            s = s[2..];
        var value = Util.GetHexValue64(s);

        var hub = SysCord<T>.Runner.Hub;
        var r = new SeedSearchResult(Z3SearchResult.Success, value, -1, hub.Config.SeedCheckSWSH.ResultDisplayMode);
        var embed = new EmbedBuilder { Color = Color.LighterGrey };
        embed.AddField($"Seed: 0x{value:X16}", r.ToString());
        await FollowupAsync($"Here are the details for `{r.Seed:X16}`:", embed: embed.Build()).ConfigureAwait(false);
    }

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("seed-list", "Prints the users in the Seed Check queue.")]
    [RequireUserPermission(ChannelPermission.BypassSlowmode)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    public async Task GetSeedListAsync()
    {
        if (!await RequireAsync(CheckSudo(out var e), e).ConfigureAwait(false))
            return;

        var embed = new EmbedBuilder();
        embed.AddField("Pending Trades", Info.GetTradeList(PokeRoutineType.SeedCheck));
        await RespondAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }

}
