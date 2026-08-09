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
    public async Task HelpAsync(string? command = null)
    {
        var builder = new EmbedBuilder { Color = Color.Blue };
        if (string.IsNullOrWhiteSpace(command))
        {
            builder.Description = "These are the commands you can use:";
            foreach (var group in service.SlashCommands.GroupBy(x => x.Module.Name))
            {
                var names = group.Select(x => x.Name).Distinct().Order();
                var value = string.Join('\n', names);
                if (!string.IsNullOrWhiteSpace(value))
                    builder.AddField(group.Key.Replace("Module", string.Empty).Replace("`1", string.Empty), value);
            }
        }
        else
        {
            var matches = service.SlashCommands.Where(x =>
                x.Name.Equals(command, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(command, StringComparison.OrdinalIgnoreCase)).ToList();

            int added = 0;
            // Use a different description.
            builder.Description = $"Here are some commands like **{command}**:";
            foreach (var cmd in matches)
            {
                var isOwner = cmd.Module.Attributes.Any(z => z.GetType() == typeof(RequireOwnerAttribute));
                if (isOwner && !CheckSudo(out _))
                    continue;
                if (cmd.Module.GetType().IsAssignableFrom(typeof(SudoModuleBase)) && !CheckSudo(out _))
                    continue;

                var parameters = GetParameters(cmd.Parameters);
                builder.AddField(cmd.Name, $"Summary: {cmd.Description}\nParameters:\n{parameters}");
                added++;
            }
            if (added == 0)
            {
                await RespondAsync($"Sorry, I couldn't find a command like **{command}**.", ephemeral: true).ConfigureAwait(false);
                return;
            }
        }
        await RespondAsync("Help has arrived!", ephemeral: true, embed: builder.Build()).ConfigureAwait(false);
    }

    private static string GetParameters(IReadOnlyList<SlashCommandParameterInfo> para)
    {
        if (para.Count == 0)
            return "None";
        return string.Join('\n', para.Select(p => $"- {p.Name} ({p.Description})"));
    }
}
