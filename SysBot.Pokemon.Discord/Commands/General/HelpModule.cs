using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class HelpModule(InteractionService service) : SlashModuleBase
{
    [SlashCommand("help", "Lists available slash commands.")]
    public async Task HelpAsync(
        [Summary(nameof(commandName), "Command name to get help for. Leave blank to fetch all available.")] string? commandName = null)
    {
        var builder = new EmbedBuilder { Color = Color.Blue };
        var provider = SysCordSettings.ServiceProvider;
        if (string.IsNullOrWhiteSpace(commandName))
        {
            // There will be at least one command always (`help` was just used!).
            await BuildCommandsAll(builder, provider).ConfigureAwait(false);
        }
        else
        {
            var added = await BuildCommandsMatching(builder, provider, commandName).ConfigureAwait(false);
            if (added == 0)
            {
                var message = $"Sorry, I couldn't find a command like {Format.Bold(commandName)}.";
                await RespondAsync(message, ephemeral: true).ConfigureAwait(false);
                return;
            }
            // Use a different description.
            builder.Description = $"Here are some commands like {Format.Bold(commandName)}:";
        }
        await RespondAsync("Help has arrived!", ephemeral: true, embed: builder.Build()).ConfigureAwait(false);
    }

    private async Task<int> BuildCommandsMatching(EmbedBuilder builder, IServiceProvider provider, string commandName)
    {
        var matches = service.SlashCommands.Where(x =>
            x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains(commandName, StringComparison.OrdinalIgnoreCase)).ToList();

        int added = 0;
        foreach (var cmd in matches)
        {
            var check = await cmd.CheckPreconditionsAsync(Context, provider).ConfigureAwait(false);
            if (!check.IsSuccess)
                continue;

            var parameters = GetParameters(cmd.Parameters);
            builder.AddField(cmd.Name, $"Summary: {cmd.Description}\nParameters:\n{parameters}");
            added++;
        }

        return added;
    }

    private async Task BuildCommandsAll(EmbedBuilder builder, IServiceProvider provider)
    {
        builder.Description = "These are the commands you can use:";
        var list = GetAvailableCommands(service.SlashCommands, Context, provider);
        var grouped = list.GroupBy(x => x.Module.Name).ConfigureAwait(false);

        await foreach (var group in grouped.ConfigureAwait(false))
        {
            var names = group.Select(x => x.Name).Distinct().Order();
            var value = string.Join('\n', names);
            if (value.Length == 0)
                continue; // Shouldn't happen, but just in case.
            if (!string.IsNullOrWhiteSpace(value))
                builder.AddField(ReusableActions.GetModuleName(group.Key), value);
        }
    }

    private static async IAsyncEnumerable<SlashCommandInfo> GetAvailableCommands(IEnumerable<SlashCommandInfo> possible,
        IInteractionContext context, IServiceProvider provider)
    {
        foreach (var cmd in possible)
        {
            var result = await cmd.CheckPreconditionsAsync(context, provider).ConfigureAwait(false);
            if (result.IsSuccess)
                yield return cmd;
        }
    }

    private static string GetParameters(IReadOnlyList<SlashCommandParameterInfo> para)
    {
        if (para.Count == 0)
            return "None";
        return string.Join('\n', para.Select(p => $"- {p.Name} ({p.Description})"));
    }
}
