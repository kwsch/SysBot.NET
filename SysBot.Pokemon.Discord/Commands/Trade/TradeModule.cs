using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[RequireContext(ContextType.Guild)]
public class TradeModule<T> : SlashModuleBase where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [SlashCommand("trade", "Trade a Pokémon file or a Showdown Set.")]
    public async Task TradeAsync(
        [Summary(nameof(code), "Optional; leave blank for a random code")] int? code = null,
        [Summary(nameof(set), "Provide if you want to convert text.")] string? set = null,
        [Summary(nameof(file), "Provide if you want to trade a file.")] IAttachment? file = null)
    {
        if (!CheckQueueAccess(PokeRoutineType.LinkTrade, out var error))
        {
            await RespondAsync(error, ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (set is not null && file is not null)
        {
            await RespondAsync("Provide either a Showdown Set or a Pokémon file, not both.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        if (set is null && file is null)
        {
            await RespondAsync("Provide a Showdown Set or attach a Pokémon file.", ephemeral: true).ConfigureAwait(false);
            return;
        }

        // Generating from a set might take longer than 3 seconds for some hosts with slower computers.
        await DeferAsync(ephemeral: true).ConfigureAwait(false);

        var tradeCode = code ?? Info.GetRandomTradeCode();

        if (file is not null)
        {
            await TradeAttachmentAsync(tradeCode, file, Context).ConfigureAwait(false);
            return;
        }

        await TradeShowdownAsync(tradeCode, set!, Context).ConfigureAwait(false);
    }

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("trade-list", "Prints the users in the trade queues.")]
    [RequireUserPermission(ChannelPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    public async Task GetTradeListAsync()
    {
        if (!CheckSudo(out var error))
        {
            await RespondAsync(error, ephemeral: true).ConfigureAwait(false);
            return;
        }

        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = Info.GetTradeList(PokeRoutineType.LinkTrade);
            x.IsInline = false;
        });

        await RespondAsync("These are the users who are currently waiting:", ephemeral: true, embed: embed.Build()).ConfigureAwait(false);
    }

    [SlashCommand("ban-trade", "Ban an Online ID from trading.")]
    [RequireUserPermission(ChannelPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    public async Task BanTradeAsync(
        [Summary(nameof(nnid), "The in-game/online ID of the user to ban from trading.")] ulong nnid,
        [Summary(nameof(reason), "The reason for banning the user.")] string reason)
    {
        if (!CheckSudo(out var error))
        {
            await RespondAsync(error, ephemeral: true).ConfigureAwait(false);
            return;
        }

        await DeferAsync().ConfigureAwait(false);
        SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew(GetReference(nnid, reason));
        await FollowupAsync($"Done. Online ID {nnid} has been banned for reason: {reason}").ConfigureAwait(false);
    }

    private async Task TradeShowdownAsync(int code, string content, SocketInteractionContext user)
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0 || set.Species is 0)
        {
            var sb = new StringBuilder(128);
            sb.AppendLine("Unable to parse Showdown Set.");
            var invalidlines = set.InvalidLines;
            if (invalidlines.Count != 0)
            {
                var localization = BattleTemplateParseErrorLocalization.Get();
                sb.AppendLine("Invalid lines detected:\n```yml");
                foreach (var line in invalidlines)
                {
                    var error = line.Humanize(localization);
                    sb.AppendLine(error);
                }
                sb.AppendLine("```");
            }
            if (set.Species is 0)
                sb.AppendLine("Species could not be identified. Check your spelling.");

            var msg = sb.ToString();
            await FollowupAsync(msg, ephemeral: true).ConfigureAwait(false);
            return;
        }

        try
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            var pkm = sav.GetLegal(template, out var result);
            var la = new LegalityAnalysis(pkm);
            var spec = GameInfo.Strings.Species[template.Species];
            pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
            if (pkm is not T pk || !la.Valid)
            {
                var reason = result switch
                {
                    "Timeout" => $"That {spec} set took too long to generate.",
                    "VersionMismatch" => "Request refused: PKHeX and Auto-Legality Mod version mismatch.",
                    _ => $"I wasn't able to create a {spec} from that set.",
                };
                var imsg = $"Oops! {reason}";
                if (result == "Failed")
                    imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                await FollowupAsync(imsg).ConfigureAwait(false);
                return;
            }

            pk.ResetPartyStats();
            await AddTradeToQueueAsync(code, pk, user).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex);
            var msg = $"""
                       Oops! An unexpected problem happened with this Showdown Set:
                       {Format.Code(string.Join('\n', set.GetSetLines()),"yml")}
                       """;
            await FollowupAsync(msg).ConfigureAwait(false);
        }
    }

    private async Task TradeAttachmentAsync(int code, IAttachment attachment, SocketInteractionContext user)
    {
        var att = await attachment.DownloadEntityAsync().ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await FollowupAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }

        await AddTradeToQueueAsync(code, pk, user).ConfigureAwait(false);
    }

    private static T? GetRequest(Download<PKM> dl)
    {
        if (!dl.Success)
            return null;
        return dl.Data switch
        {
            null => null,
            T pk => pk,
            _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
        };
    }

    private async Task AddTradeToQueueAsync(int code, T pk, SocketInteractionContext context)
    {
        var la = new LegalityAnalysis(pk);
        if (!la.Valid)
        {
            await FollowupAsync($"{typeof(T).Name} attachment is not legal, and cannot be traded!").ConfigureAwait(false);
            return;
        }

        var enc = la.EncounterOriginal;
        if (!pk.CanBeTraded(enc))
        {
            await FollowupAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
            return;
        }
        var cfg = Info.Hub.Config.Trade;
        if (cfg.DisallowNonNatives && (enc.Context != pk.Context || pk.GO))
        {
            await FollowupAsync($"{typeof(T).Name} attachment is not native, and cannot be traded!").ConfigureAwait(false);
            return;
        }

        if (cfg.DisallowTracked && pk is IHomeTrack { HasTracker: true })
        {
            await FollowupAsync($"{typeof(T).Name} attachment is tracked by HOME, and cannot be traded!").ConfigureAwait(false);
            return;
        }

        var sig = GetSignificance(context.User);
        await QueueHelper<T>.AddToQueueAsync(Context, code, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, context).ConfigureAwait(false);
    }
}
