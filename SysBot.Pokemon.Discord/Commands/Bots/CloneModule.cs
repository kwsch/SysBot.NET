using Discord;
using Discord.Commands;
using PKHeX.Core;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Clone trades")]
public class CloneModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public async Task CloneAsync(int code)
    {
        var sig = Context.User.GetFavor();
        // Execute the queue addition without capturing response
        QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone);

        // Optional: Send a confirmation message if needed
        var confirmationMessage = await ReplyAsync("Processing your clone request...").ConfigureAwait(false);

        // Delay for 5 seconds
        await Task.Delay(5000).ConfigureAwait(false);

        // Delete user message
        if (Context.Message is IUserMessage userMessage)
            await userMessage.DeleteAsync().ConfigureAwait(false);

        // Delete bot confirmation message
        if (confirmationMessage != null)
            await confirmationMessage.DeleteAsync().ConfigureAwait(false);
    }

    [Command("clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public async Task CloneAsync([Summary("Trade Code")][Remainder] string code)
    {
        int tradeCode = Util.ToInt32(code);
        var sig = Context.User.GetFavor();

        // Execute the queue addition without capturing response
        QueueHelper<T>.AddToQueueAsync(Context, tradeCode == 0 ? Info.GetRandomTradeCode() : tradeCode, Context.User.Username, sig, new T(), PokeRoutineType.Clone, PokeTradeType.Clone);

        // Optional: Send a confirmation message if needed
        var confirmationMessage = await ReplyAsync("Processing your clone request...").ConfigureAwait(false);

        // Delay for 5 seconds
        await Task.Delay(5000).ConfigureAwait(false);

        // Delete user message
        if (Context.Message is IUserMessage userMessage)
            await userMessage.DeleteAsync().ConfigureAwait(false);

        // Delete bot confirmation message
        if (confirmationMessage != null)
            await confirmationMessage.DeleteAsync().ConfigureAwait(false);
    }

    [Command("clone")]
    [Alias("c")]
    [Summary("Clones the Pokémon you show via Link Trade.")]
    [RequireQueueRole(nameof(DiscordManager.RolesClone))]
    public Task CloneAsync()
    {
        var code = Info.GetRandomTradeCode();
        return CloneAsync(code);

    }

    [Command("cloneList")]
    [Alias("cl", "cq")]
    [Summary("Prints the users in the Clone queue.")]
    [RequireSudo]
    public async Task GetListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.Clone);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
    }
}
