using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public static class ReusableActions
{
    public static async Task SendPKMAsync(this IMessageChannel channel, PKM pkm, string msg = "")
    {
        var tmp = Path.Combine(Path.GetTempPath(), Util.CleanFileName(pkm.FileName));
        await File.WriteAllBytesAsync(tmp, pkm.DecryptedPartyData);
        await channel.SendFileAsync(tmp, msg).ConfigureAwait(false);
        File.Delete(tmp);
    }

    public static async Task SendPKMAsync(this IUser user, PKM pkm, string msg = "")
    {
        var tmp = Path.Combine(Path.GetTempPath(), Util.CleanFileName(pkm.FileName));
        await File.WriteAllBytesAsync(tmp, pkm.DecryptedPartyData);
        await user.SendFileAsync(tmp, msg).ConfigureAwait(false);
        File.Delete(tmp);
    }

    public static async Task RepostPKMAsShowdownAsync(this ISocketMessageChannel channel, IAttachment att, SocketUserMessage userMessage)
    {
        if (!EntityDetection.IsSizePlausible(att.Size))
            return;
        var result = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
        if (!result.Success)
            return;

        var pkm = result.Data!;
        await channel.SendPKMAsShowdownSetAsync(pkm, userMessage).ConfigureAwait(false);
    }

    public static RequestSignificance GetFavor(this IUser user)
    {
        var mgr = SysCordSettings.Manager;
        if (user.Id == mgr.Owner)
            return RequestSignificance.Owner;
        if (mgr.CanUseSudo(user.Id))
            return RequestSignificance.Favored;
        if (user is SocketGuildUser g)
            return mgr.GetSignificance(g.Roles.Select(z => z.Name));
        return RequestSignificance.None;
    }

    public static async Task EchoAndReply(this ISocketMessageChannel channel, string msg)
    {
        // Announce it in the channel the command was entered only if it's not already an echo channel.
        EchoUtil.Echo(msg);
        if (!EchoModule.IsEchoChannel(channel))
            await channel.SendMessageAsync(msg).ConfigureAwait(false);
    }

    public static async Task SendPKMAsShowdownSetAsync(this ISocketMessageChannel channel, PKM pkm, SocketUserMessage userMessage)
    {
        var txt = GetFormattedShowdownText(pkm);
        var botMessage = await channel.SendMessageAsync(txt).ConfigureAwait(false); // Capture the bot's message

        // Send a warning message
        var warningMessage = await channel.SendMessageAsync("This message will self-destruct in 15 seconds. Please copy your data.").ConfigureAwait(false);

        // Wait for 2 seconds
        await Task.Delay(2000).ConfigureAwait(false);

        // Delete the user's message
        await userMessage.DeleteAsync().ConfigureAwait(false);

        // Wait for another 13 seconds (total 15 seconds)
        await Task.Delay(13000).ConfigureAwait(false);

        // Delete the bot's messages
        await botMessage.DeleteAsync().ConfigureAwait(false);
        await warningMessage.DeleteAsync().ConfigureAwait(false);
    }

    public static string GetFormattedShowdownText(PKM pkm)
    {
        var newShowdown = new List<string>();
        var showdown = ShowdownParsing.GetShowdownText(pkm);
        foreach (var line in showdown.Split('\n'))
            newShowdown.Add(line);

        if (pkm.IsEgg)
            newShowdown.Add("\nPokémon is an egg");
        if (pkm.Ball > (int)Ball.None)
            newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"Ball: {(Ball)pkm.Ball} Ball");
        if (pkm.IsShiny)
        {
            var index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
            if (pkm.ShinyXor == 0 || pkm.FatefulEncounter)
                newShowdown[index] = "Shiny: Square\r";
            else newShowdown[index] = "Shiny: Star\r";
        }

        newShowdown.InsertRange(1, new string[] { $"OT: {pkm.OT_Name}", $"TID: {pkm.DisplayTID}", $"SID: {pkm.DisplaySID}", $"OTGender: {(Gender)pkm.OT_Gender}", $"Language: {(LanguageID)pkm.Language}" });
        return Format.Code(string.Join("\n", newShowdown).TrimEnd());
    }

    private static readonly string[] separator = [ ",", ", ", " " ];

    public static IReadOnlyList<string> GetListFromString(string str)
    {
        // Extract comma separated list
        return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    public static string StripCodeBlock(string str) => str
        .Replace("`\n", "")
        .Replace("\n`", "")
        .Replace("`", "")
        .Trim();
}
