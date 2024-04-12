using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

[Summary("Queues new Link Code trades")]
public class TradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

    [Command("tradeList")]
    [Alias("tl")]
    [Summary("Zeigt die Benutzer in den Handelswarteschlangen an.")]
    [RequireSudo]
    public async Task GetTradeListAsync()
    {
        string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
        var embed = new EmbedBuilder();
        embed.AddField(x =>
        {
            x.Name = "Pending Trades";
            x.Value = msg;
            x.IsInline = false;
        });
        await ReplyAsync("Dies sind die Benutzer, die derzeit warten:", embed: embed.Build()).ConfigureAwait(false);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Bringt den Bot dazu, dir die angegebene Pokémon-Datei zu geben.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsyncAttach([Summary("Trade Code")] int code)
    {
        var sig = Context.User.GetFavor();
        return TradeAsyncAttach(code, sig, Context.User);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Bringt den Bot dazu, dir ein Pokémon zu geben, das aus dem angegebenen Showdown-Set umgewandelt wurde.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
    {
        content = ReusableActions.StripCodeBlock(content);
        var set = new ShowdownSet(content);
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (set.InvalidLines.Count != 0)
        {
            var msg = $"Showdown-Set kann nicht analysiert werden:\n{string.Join("\n", set.InvalidLines)}";
            await ReplyAsync(msg).ConfigureAwait(false);
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
                var reason = result == "Timeout" ? $"Die Erstellung dieses {spec}-Satzes hat zu lange gedauert." : result == "VersionMismatch" ? "Anfrage abgelehnt: PKHeX und Auto-Legality Mod Version stimmen nicht überein." : $"Ich war nicht in der Lage, eine {spec} aus diesem Datensatz zu erstellen.";
                var imsg = $"Oops! {reason}";
                if (result == "Failed")
                    imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                await ReplyAsync(imsg).ConfigureAwait(false);
                return;
            }
            pk.ResetPartyStats();

            var sig = Context.User.GetFavor();
            await AddTradeToQueueAsync(code, Context.User.Username, pk, sig, Context.User).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TradeModule<T>));
            var msg = $"Ups! Bei diesem Showdown-Set ist ein unerwartetes Problem aufgetreten:\n```{string.Join("\n", set.GetSetLines())}```";
            await ReplyAsync(msg).ConfigureAwait(false);
        }
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Bringt den Bot dazu, dir ein Pokémon zu geben, das aus dem angegebenen Showdown-Set umgewandelt wurde.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
    {
        var code = Info.GetRandomTradeCode();
        return TradeAsync(code, content);
    }

    [Command("trade")]
    [Alias("t")]
    [Summary("Bringt den Bot dazu, Ihnen die angehängte Datei zu handeln.")]
    [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
    public Task TradeAsyncAttach()
    {
        var code = Info.GetRandomTradeCode();
        return TradeAsyncAttach(code);
    }

    [Command("banTrade")]
    [Alias("bt")]
    [RequireSudo]
    public async Task BanTradeAsync([Summary("Online ID")] ulong nnid, string comment)
    {
        SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew(new[] { GetReference(nnid, comment) });
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(ulong id, string comment) => new()
    {
        ID = id,
        Name = id.ToString(),
        Comment = $"Hinzugefügt von {Context.User.Username} am {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
    };

    [Command("tradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Bringt den Bot dazu, mit dem genannten Benutzer die angehängte Datei zu handeln.")]
    [RequireSudo]
    public async Task TradeAsyncAttachUser([Summary("Trade Code")] int code, [Remainder] string _)
    {
        if (Context.Message.MentionedUsers.Count > 1)
        {
            await ReplyAsync("Zu viele Erwähnungen. Jeweils einen Benutzer in die Warteschlange stellen.").ConfigureAwait(false);
            return;
        }

        if (Context.Message.MentionedUsers.Count == 0)
        {
            await ReplyAsync("Dazu muss ein Benutzer genannt werden.").ConfigureAwait(false);
            return;
        }

        var usr = Context.Message.MentionedUsers.ElementAt(0);
        var sig = usr.GetFavor();
        await TradeAsyncAttach(code, sig, usr).ConfigureAwait(false);
    }

    [Command("tradeUser")]
    [Alias("tu", "tradeOther")]
    [Summary("Bringt den Bot dazu, mit dem erwähnten Benutzer die angehängte Datei zu handeln.")]
    [RequireSudo]
    public Task TradeAsyncAttachUser([Remainder] string _)
    {
        var code = Info.GetRandomTradeCode();
        return TradeAsyncAttachUser(code, _);
    }

    private async Task TradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr)
    {
        var attachment = Context.Message.Attachments.FirstOrDefault();
        if (attachment == default)
        {
            await ReplyAsync("Keine Anlage vorhanden!").ConfigureAwait(false);
            return;
        }

        var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
        var pk = GetRequest(att);
        if (pk == null)
        {
            await ReplyAsync("Der mitgelieferte Anhang ist nicht mit diesem Modul kompatibel!").ConfigureAwait(false);
            return;
        }

        await AddTradeToQueueAsync(code, usr.Username, pk, sig, usr).ConfigureAwait(false);
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

    private async Task AddTradeToQueueAsync(int code, string trainerName, T pk, RequestSignificance sig, SocketUser usr)
    {
        if (!pk.CanBeTraded())
        {
            // Disallow anything that cannot be traded from the game (e.g. Fusions).
            await ReplyAsync("Vorgesehener Pokémon-Inhalt ist für den Handel gesperrt!").ConfigureAwait(false);
            return;
        }

        var cfg = Info.Hub.Config.Trade;
        var la = new LegalityAnalysis(pk);
        if (!la.Valid)
        {
            // Disallow trading illegal Pokémon.
            await ReplyAsync($"{typeof(T).Name} Anhang ist nicht legal, und kann nicht gehandelt werden!").ConfigureAwait(false);
            return;
        }
        if (cfg.DisallowNonNatives && (la.EncounterOriginal.Context != pk.Context || pk.GO))
        {
            // Allow the owner to prevent trading entities that require a HOME Tracker even if the file has one already.
            await ReplyAsync($"{typeof(T).Name} Anlage ist nicht einheimisch, und kann nicht gehandelt werden!").ConfigureAwait(false);
            return;
        }
        if (cfg.DisallowTracked && pk is IHomeTrack { HasTracker: true })
        {
            // Allow the owner to prevent trading entities that already have a HOME Tracker.
            await ReplyAsync($"{typeof(T).Name} Die Anlage wurde von HOME registriert und kann nicht gehandelt werden!").ConfigureAwait(false);
            return;
        }

        await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr).ConfigureAwait(false);
    }
}
