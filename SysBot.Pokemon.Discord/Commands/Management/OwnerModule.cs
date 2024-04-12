using Discord;
using Discord.Commands;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class OwnerModule<T> : SudoModule<T> where T : PKM, new()
{
    [Command("addSudo")]
    [Summary("Fügt erwähnten Benutzer zu globalem sudo hinzu")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task SudoUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.AddIfNew(objects);
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("removeSudo")]
    [Summary("Entfernt den erwähnten Benutzer aus dem globalen sudo")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task RemoveSudoUsers([Remainder] string _)
    {
        var users = Context.Message.MentionedUsers;
        var objects = users.Select(GetReference);
        SysCordSettings.Settings.GlobalSudoList.RemoveAll(z => objects.Any(o => o.ID == z.ID));
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("addChannel")]
    [Summary("Fügt einen Kanal in die Liste der Kanäle ein, die Befehle akzeptieren.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task AddChannel()
    {
        var obj = GetReference(Context.Message.Channel);
        SysCordSettings.Settings.ChannelWhitelist.AddIfNew(new[] { obj });
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("removeChannel")]
    [Summary("Entfernt einen Kanal aus der Liste der Kanäle, die Befehle akzeptieren.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task RemoveChannel()
    {
        var obj = GetReference(Context.Message.Channel);
        SysCordSettings.Settings.ChannelWhitelist.RemoveAll(z => z.ID == obj.ID);
        await ReplyAsync("Erledigt.").ConfigureAwait(false);
    }

    [Command("leave")]
    [Alias("bye")]
    [Summary("Verlässt den aktuellen Server.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task Leave()
    {
        await ReplyAsync("Auf Wiedersehen.").ConfigureAwait(false);
        await Context.Guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveguild")]
    [Alias("lg")]
    [Summary("Verlässt die Gilde anhand der angegebenen ID.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task LeaveGuild(string userInput)
    {
        if (!ulong.TryParse(userInput, out ulong id))
        {
            await ReplyAsync("Bitte legen Sie eine gültige Gilden-ID vor.").ConfigureAwait(false);
            return;
        }

        var guild = Context.Client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            await ReplyAsync($"Die gegebene Eingabe ({userInput}) ist keine gültige Gilden-ID oder der Bot ist nicht in der angegebenen Gilde.").ConfigureAwait(false);
            return;
        }

        await ReplyAsync($"Verlassen von {guild}.").ConfigureAwait(false);
        await guild.LeaveAsync().ConfigureAwait(false);
    }

    [Command("leaveall")]
    [Summary("Verlässt alle Server, auf denen sich der Bot gerade befindet.")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task LeaveAll()
    {
        await ReplyAsync("Alle Server werden verlassen.").ConfigureAwait(false);
        foreach (var guild in Context.Client.Guilds)
        {
            await guild.LeaveAsync().ConfigureAwait(false);
        }
    }

    [Command("sudoku")]
    [Alias("kill", "shutdown")]
    [Summary("Beendet den gesamten Prozess selbst!")]
    [RequireOwner]
    // ReSharper disable once UnusedParameter.Global
    public async Task ExitProgram()
    {
        await Context.Channel.EchoAndReply("Herunterfahren... auf Wiedersehen! **Bot-Dienste gehen offline.**").ConfigureAwait(false);
        Environment.Exit(0);
    }

    private RemoteControlAccess GetReference(IUser channel) => new()
    {
        ID = channel.Id,
        Name = channel.Username,
        Comment = $"Hinzugefügt durch {Context.User.Username} am {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Hinzugefügt durch {Context.User.Username} am {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
