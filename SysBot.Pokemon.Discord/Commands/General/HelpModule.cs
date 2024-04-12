using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class HelpModule(CommandService Service) : ModuleBase<SocketCommandContext>
{
    [Command("help")]
    [Alias("hilfe")]
    [Summary("Listet verfügbare Befehle auf.")]
    public async Task HelpAsync()
    {
        var builder = new EmbedBuilder
        {
            Color = new Color(114, 137, 218),
            Description = "Dies sind die Befehle, die Sie verwenden können:",
        };

        var mgr = SysCordSettings.Manager;
        var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
        var owner = app.Owner.Id;
        var uid = Context.User.Id;

        foreach (var module in Service.Modules)
        {
            string? description = null;
            HashSet<string> mentioned = [];
            foreach (var cmd in module.Commands)
            {
                var name = cmd.Name;
                if (mentioned.Contains(name))
                    continue;
                if (cmd.Attributes.Any(z => z is RequireOwnerAttribute) && owner != uid)
                    continue;
                if (cmd.Attributes.Any(z => z is RequireSudoAttribute) && !mgr.CanUseSudo(uid))
                    continue;

                mentioned.Add(name);
                var result = await cmd.CheckPreconditionsAsync(Context).ConfigureAwait(false);
                if (result.IsSuccess)
                    description += $"{cmd.Aliases[0]}\n";
            }
            if (string.IsNullOrWhiteSpace(description))
                continue;

            var moduleName = module.Name;
            var gen = moduleName.IndexOf('`');
            if (gen != -1)
                moduleName = moduleName[..gen];

            builder.AddField(x =>
            {
                x.Name = moduleName;
                x.Value = description;
                x.IsInline = false;
            });
        }

        await ReplyAsync("Help has arrived!", false, builder.Build()).ConfigureAwait(false);
    }

    [Command("help")]
    [Summary("Listet Informationen über einen bestimmten Befehl auf.")]
    public async Task HelpAsync([Summary("Der Befehl, für den Sie Hilfe benötigen")] string command)
    {
        var result = Service.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Entschuldigung, ich konnte keinen Befehl wie **{command}** finden.").ConfigureAwait(false);
            return;
        }

        var builder = new EmbedBuilder
        {
            Color = new Color(114, 137, 218),
            Description = $"Hier sind einige Befehle wie **{command}**:",
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
        return $"Zusammenfassung: {cmd.Summary}\nParameter: {GetParameterSummary(cmd.Parameters)}";
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
