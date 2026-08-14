using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

public partial class TradeModule<T>
{
    [SlashCommand("list", "Prints the users in the trade queues.")]
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [RequireSudo]
    public async Task GetTradeListAsync()
    {
        var embed = new EmbedBuilder { Color = Color.LightGrey, Title = nameof(PokeRoutineType.LinkTrade) };
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = Info.GetTradeList(PokeRoutineType.LinkTrade);
            x.IsInline = false;
        });

        await RespondAsync("These are the users who are currently waiting:", ephemeral: true, embed: embed.Build()).ConfigureAwait(false);
    }

    [SlashCommand("ban", "Ban an Online ID from trading.")]
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [RequireSudo]
    public async Task BanTradeAsync(
        [Summary(nameof(nnid), "The in-game/online ID of the user to ban from trading.")] ulong nnid,
        [Summary(nameof(reason), "The reason for banning the user.")] string reason)
    {
        // Display not-ephemeral message to the sudo user, since this is a sudo command and they should be aware of the action being taken.
        await DeferAsync().ConfigureAwait(false);
        SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew(GetReference(nnid, reason));
        await FollowupAsync($"Done. Online ID {nnid} has been banned for reason: {reason}").ConfigureAwait(false);
    }
}
