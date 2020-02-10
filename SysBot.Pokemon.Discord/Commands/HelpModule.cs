using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

namespace SysBot.Pokemon.Discord
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public HelpModule(CommandService service)
        {
            _service = service;
        }

        [Command("help")]
        [Summary("Lists available commands.")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use:"
            };

            foreach (var module in _service.Modules)
            {
                string? description = null;
                HashSet<string> mentioned = new HashSet<string>();
                foreach (var cmd in module.Commands)
                {
                    var name = cmd.Name;
                    if (mentioned.Contains(name))
                        continue;
                    mentioned.Add(name);
                    var result = await cmd.CheckPreconditionsAsync(Context).ConfigureAwait(false);
                    if (result.IsSuccess)
                        description += $"{cmd.Aliases[0]}\n";
                }
                if (string.IsNullOrWhiteSpace(description))
                    continue;

                builder.AddField(x =>
                {
                    x.Name = module.Name;
                    x.Value = description;
                    x.IsInline = false;
                });
            }

            await ReplyAsync("Help has arrived!", false, builder.Build()).ConfigureAwait(false);
        }

        [Command("help")]
        [Summary("Lists information about a specific command.")]
        public async Task HelpAsync([Summary("The command you want help for")] string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.").ConfigureAwait(false);
                return;
            }

            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**:"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = GetCommandSummary(cmd);
                    x.IsInline = false;
                });
            }

            await ReplyAsync("Help has arrived!", false, builder.Build()).ConfigureAwait(false);
        }

        private static string GetCommandSummary(CommandInfo cmd)
        {
            return $"Summary: {cmd.Summary}\nParameters: {GetParameterSummary(cmd.Parameters)}";
        }

        private static string GetParameterSummary(IReadOnlyList<ParameterInfo> p)
        {
            if (p.Count == 0)
                return "None";
            return $"{p.Count}\n- " + string.Join("\n- ", p.Select(GetParameterSummary));
        }

        private static string GetParameterSummary(ParameterInfo z)
        {
            var result = z.Name;
            if (!string.IsNullOrWhiteSpace(z.Summary))
                result += $" ({z.Summary})";
            return result;
        }
    }
}