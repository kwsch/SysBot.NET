using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class SudoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
{
    [Command("blacklist")]
    [Summary("Nimmt einen erwähnten Discord-Benutzer auf die Negativ-Liste.")]
    [RequireSudo]
    // ReSharper disable once UnusedParameter.Global
    public async Task BlackListUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("blacklistComment")]
    [Summary("Fügt einen Kommentar für eine Discord-Benutzer-ID auf der Negativ-Liste hinzu.")]
    [RequireSudo]
    // ReSharper disable once UnusedParameter.Global
    public async Task BlackListUsers(ulong id, [Remainder] string comment)
    {
        var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Es konnte kein Benutzer mit dieser ID gefunden werden ({id}).").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"cErledigt. Vorhandener Kommentar ({oldComment}) wurde in ({comment}) geändert.").ConfigureAwait(false);
    }

    [Command("unblacklist")]
    [Summary("Entfernt einen erwähnten Discord-Benutzer von der Sperrliste.")]
    [RequireSudo]
    // ReSharper disable once UnusedParameter.Global
    public async Task UnBlackListUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("blacklistId")]
    [Summary("Listet Discord-Benutzer-IDs auf eine Sperrliste. (Nützlich, wenn der Benutzer nicht auf dem Server ist).")]
    [RequireSudo]
    public async Task BlackListIDs([Summary("Durch Komma getrennte Discord-IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);
        SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("unBlacklistId")]
    [Summary("Entfernt Discord-Benutzer-IDs von der Sperrliste. (Nützlich, wenn der Benutzer nicht auf dem Server ist).")]
    [RequireSudo]
    public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        SysCordSettings.Settings.UserBlacklist.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("blacklistSummary")]
    [Alias("printBlacklist", "blacklistPrint")]
    [Summary("Zeigt die Liste der auf der Sperrliste stehenden Discord-Benutzer an.")]
    [RequireSudo]
    public async Task PrintBlacklist()
    {
        var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [Command("banID")]
    [Summary("Verbietet Online-Benutzer-IDs.")]
    [RequireSudo]
    public async Task BanOnlineIDs([Summary("Durch Komma getrennte Online-IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var objects = IDs.Select(GetReference);

        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        hub.Config.TradeAbuse.BannedIDs.AddIfNew(objects);
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("bannedIDComment")]
    [Summary("Fügt einen Kommentar für eine gesperrte Online-Benutzer-ID hinzu.")]
    [RequireSudo]
    public async Task BanOnlineIDs(ulong id, [Remainder] string comment)
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var obj = hub.Config.TradeAbuse.BannedIDs.List.Find(z => z.ID == id);
        if (obj is null)
        {
            await ReplyAsync($"Es konnte kein Benutzer mit dieser Online-ID ({id}) gefunden werden.").ConfigureAwait(false);
            return;
        }

        var oldComment = obj.Comment;
        obj.Comment = comment;
        await ReplyAsync($"Erledigt. Vorhandener Kommentar ({oldComment}) wurde in ({comment}) geändert.").ConfigureAwait(false);
    }

    [Command("unbanID")]
    [Summary("entperrt Online-Benutzer-IDs.")]
    [RequireSudo]
    public async Task UnBanOnlineIDs([Summary("Durch Komma getrennte Online-IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        hub.Config.TradeAbuse.BannedIDs.RemoveAll(z => IDs.Any(o => o == z.ID));
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("bannedIDSummary")]
    [Alias("printBannedID", "bannedIDPrint")]
    [Summary("Gibt die Liste der gesperrten Online-IDs aus.")]
    [RequireSudo]
    public async Task PrintBannedOnlineIDs()
    {
        var me = SysCord<T>.Runner;
        var hub = me.Hub;
        var lines = hub.Config.TradeAbuse.BannedIDs.Summarize();
        var msg = string.Join("\n", lines);
        await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
    }

    [Command("forgetUser")]
    [Alias("forget")]
    [Summary("Vergisst Benutzer, die zuvor angetroffen wurden.")]
    [RequireSudo]
    public async Task ForgetPreviousUser([Summary("Durch Komma getrennte Online-IDs")][Remainder] string content)
    {
        var IDs = GetIDs(content);
        foreach (var ID in IDs)
        {
            PokeRoutineExecutorBase.PreviousUsers.RemoveAllNID(ID);
            PokeRoutineExecutorBase.PreviousUsersDistribution.RemoveAllNID(ID);
        }
        await ReplyAsync("Done.").ConfigureAwait(false);
    }

    [Command("previousUserSummary")]
    [Alias("prevUsers")]
    [Summary("Gibt eine Liste der bisher angetroffenen Benutzer aus.")]
    [RequireSudo]
    public async Task PrintPreviousUsers()
    {
        bool found = false;
        var lines = PokeRoutineExecutorBase.PreviousUsers.Summarize().ToList();
        if (lines.Count != 0)
        {
            found = true;
            var msg = "Frühere Benutzer:\n" + string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        lines = PokeRoutineExecutorBase.PreviousUsersDistribution.Summarize().ToList();
        if (lines.Count != 0)
        {
            found = true;
            var msg = "Frühere Verteilungsbenutzer:\n" + string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }
        if (!found)
            await ReplyAsync("Keine vorherigen Benutzer gefunden.").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IUser channel) => new()
    {
        ID = channel.Id,
        Name = channel.Username,
        Comment = $"Hinzugefügt durch {Context.User.Username} am {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(ulong id) => new()
    {
        ID = id,
        Name = "Manual",
        Comment = $"Hinzugefügt durch {Context.User.Username} am {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    protected static IEnumerable<ulong> GetIDs(string content)
    {
        return content.Split([",", ", ", " "], StringSplitOptions.RemoveEmptyEntries)
            .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
    }
}
