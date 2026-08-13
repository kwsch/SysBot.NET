using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord;

[Group("trade", "Commands for starting a trade session with the bot.")]
[RequireContext(ContextType.Guild)]
[RequireOpenDms]
public class TradeModule<T> : SlashModuleBase where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    private const string TradeModalId = "trade-set";

    [SlashCommand("set", "Trade a Showdown Set.")]
    [RequireQueueRole(PokeRoutineType.LinkTrade)]
    public async Task TradeSetAsync() => await RespondWithModalAsync<TradeSetModal>(TradeModalId).ConfigureAwait(false);

    private const string TradeTextModalId = "trade-text";
    [SlashCommand("text", "Trade a Base64 text file input.")]
    [RequireQueueRole(PokeRoutineType.LinkTrade)]
    public async Task TradeTextAsync() => await RespondWithModalAsync<TradeBase64Modal>(TradeTextModalId).ConfigureAwait(false);

    /// <summary>
    /// Handles the submission of the <see cref="TradeSetModal"/>, validating the input and processing the trade.
    /// </summary>
    [ModalInteraction(TradeModalId, ignoreGroupNames: true)]
    public async Task TradeSetModalAsync(TradeSetModal modal)
    {
        // Re-check if the queue closed in the time between opening the modal and entering the info.
        if (!await CheckQueue().ConfigureAwait(false))
            return;

        // Sanity check their inputs.
        var code = modal.Code;

        if (!await Context.IsTradeCodeValidOrEmpty(code).ConfigureAwait(false))
            return;

        // Convert, then join the queue.
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var tradeCode = string.IsNullOrWhiteSpace(code)
            ? Info.GetRandomTradeCode()
            : int.Parse(code); // already validated above, so safe to parse

        await TradeTextAsync(tradeCode, modal.Showdown, Context).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the submission of the <see cref="TradeBase64Modal"/>, which allows users to trade a Base64 text file input.
    /// </summary>
    [ModalInteraction(TradeTextModalId, ignoreGroupNames: true)]
    public async Task TradeTextModalAsync(TradeBase64Modal modal)
    {
        // Re-check if the queue closed in the time between opening the modal and entering the info.
        if (!await CheckQueue().ConfigureAwait(false))
            return;

        // Sanity check their inputs.
        var code = modal.Code;
        if (!await Context.IsTradeCodeValidOrEmpty(code).ConfigureAwait(false))
            return;

        // Convert, then join the queue.
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var tradeCode = string.IsNullOrWhiteSpace(code)
            ? Info.GetRandomTradeCode()
            : int.Parse(code); // already validated above, so safe to parse

        await TradeTextAsync(tradeCode, modal.Base64, Context).ConfigureAwait(false);
    }

    // Can't have a modal with file attachment, use regular slash command instead.
    [SlashCommand("file", "Trade a Pokémon file.")]
    [RequireQueueRole(PokeRoutineType.LinkTrade)]
    public async Task TradeFileAsync(
        [Summary(nameof(file), "Attach a file to be traded to your game.")] IAttachment file,
        [Summary(nameof(code), "Optional; leave blank for a random code")] int? code = null)
    {
        // Re-check if the queue closed in the time between opening the modal and entering the info.
        if (!await CheckQueue().ConfigureAwait(false))
            return;

        if (!await Context.IsTradeCodeValidOrEmpty(code).ConfigureAwait(false))
            return;

        // Match the set->file trade command, which can take longer than 3 seconds for some hosts with slower computers.
        await DeferAsync(ephemeral: true).ConfigureAwait(false);
        var tradeCode = code ?? Info.GetRandomTradeCode(); // already validated above, so safe to take if provided

        await TradeAttachmentAsync(tradeCode, file, Context).ConfigureAwait(false);
    }

    /*
     *
     * SUDO COMMANDS BELOW
     *
     */

    [SlashCommand("list", "Prints the users in the trade queues.")]
    [DefaultMemberPermissions(GuildPermission.PrioritySpeaker)] // basic gate to hide the commands from untrusted users, but not a full sudo check
    [RequireSudo]
    public async Task GetTradeListAsync()
    {
        var embed = new EmbedBuilder { Color = Color.LightGrey };
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

    private async Task TradeTextAsync(int code, string content, IInteractionContext user)
    {
        content = ReusableActions.StripCodeBlock(content);

        // Try parsing as any language.
        if (!ShowdownParsing.TryParseAnyLanguage(content, out var set))
        {
            // Try as base64 byte[] input.
            try
            {
                var convert = Convert.FromBase64String(content);
                var pk = EntityFormat.GetFromBytes(convert, new T().Context);
                if (pk is not null && EntityConverter.ConvertToType(pk, typeof(T), out _) is T pkOfT)
                {
                    await AddTradeToQueueAsync(code, pkOfT).ConfigureAwait(false);
                    return;
                }
            }
            catch
            {
                // Probably not base64, or not a valid PKM file. Ignore and return the error message.
            }

            await FollowupAsync("Unable to detect valid data. Please check your inputs.").ConfigureAwait(false);
            return;
        }

        // Interpret set via Auto-Legality Mod, which can ingest some of the unhandled lines.
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0 || set.Species is 0)
        {
            var msg = GetInvalidSetMessage(set);
            await FollowupAsync(msg).ConfigureAwait(false);
            return;
        }

        try
        {
            await TradeShowdownAsync(code, template, user).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex);
            var msg = $"""
                       Oops! An unexpected problem happened with this Showdown Set:
                       {ReusableActions.FormatSetCode(set)}
                       """;
            await FollowupAsync(msg).ConfigureAwait(false);
        }
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

    private static string GetInvalidSetMessage(ShowdownSet set)
    {
        var sb = new StringBuilder(128);
        sb.AppendLine("Unable to parse Showdown Set.");
        var invalidlines = set.InvalidLines;
        if (invalidlines.Count != 0)
        {
            var localization = BattleTemplateParseErrorLocalization.Get();
            sb.AppendLine("Invalid lines detected:");
            AddInvalidLines(invalidlines, localization, sb);
        }
        if (set.Species is 0)
            sb.AppendLine("Species could not be identified. Check your spelling.");

        return sb.ToString();
    }

    private static void AddInvalidLines(IReadOnlyList<BattleTemplateParseError> invalidlines, BattleTemplateParseErrorLocalization localization, StringBuilder sb)
    {
        // Build a string of the invalid lines with their human-readable error messages
        // Then format it into a readable code block for Discord
        var inner = new StringBuilder();
        foreach (var line in invalidlines)
        {
            var error = line.Humanize(localization);
            inner.AppendLine(error);
        }
        sb.Append(ReusableActions.FormatSetCode(inner.ToString()));
    }

    private async Task<bool> CheckQueue()
    {
        if (SysCordSettings.HubConfig.Queues.CanQueue)
            return true;
        await RespondAsync("The trade queue has closed.", ephemeral: true).ConfigureAwait(false);
        return false;
    }

    private async Task TradeAttachmentAsync(int code, IAttachment attachment, IInteractionContext user)
    {
        var att = await attachment.DownloadEntityAsync().ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await FollowupAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
            return;
        }

        await AddTradeToQueueAsync(code, pk).ConfigureAwait(false);
    }

    private async Task TradeShowdownAsync(int code, IBattleTemplate template, IInteractionContext user)
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
        await AddTradeToQueueAsync(code, pk).ConfigureAwait(false);
    }

    private async Task AddTradeToQueueAsync(int code, T pk)
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

        await QueueHelper<T>.AddToQueueAsync(Context, code, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific).ConfigureAwait(false);
    }
}

public class TradeSetModal : IModal
{
    public string Title => "Trade Showdown Set";

    [InputLabel("Showdown Set")]
    [ModalTextInput("showdown", TextInputStyle.Paragraph, placeholder: "Paste your Showdown set here...")]
    public string Showdown { get; set; } = string.Empty;

    [RequiredInput(false)]
    [InputLabel("Trade Code (optional)")]
    [ModalTextInput("code", TextInputStyle.Short, placeholder: "Leave blank for a random code", maxLength: 8)]
    public string? Code { get; set; }
}

public class TradeBase64Modal : IModal
{
    public string Title => "Trade Base64 File";

    [InputLabel("Base64 Text")]
    [ModalTextInput("base64", TextInputStyle.Paragraph, placeholder: "Paste the Base64 text here...")]
    public string Base64 { get; set; } = string.Empty;

    [RequiredInput(false)]
    [InputLabel("Trade Code (optional)")]
    [ModalTextInput("code", TextInputStyle.Short, placeholder: "Leave blank for a random code", maxLength: 8)]
    public string? Code { get; set; }
}
