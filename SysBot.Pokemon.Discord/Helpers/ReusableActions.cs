using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class ReusableActions
    {
        public static async Task SendPKMAsync(this IMessageChannel channel, PKM pkm, string msg = "")
        {
            var tmp = Path.Combine(Path.GetTempPath(), Util.CleanFileName(pkm.FileName));
            File.WriteAllBytes(tmp, pkm.DecryptedPartyData);
            await channel.SendFileAsync(tmp, msg).ConfigureAwait(false);
            File.Delete(tmp);
        }

        public static async Task SendPKMAsync(this IUser user, PKM pkm, string msg = "")
        {
            var tmp = Path.Combine(Path.GetTempPath(), Util.CleanFileName(pkm.FileName));
            File.WriteAllBytes(tmp, pkm.DecryptedPartyData);
            await user.SendFileAsync(tmp, msg).ConfigureAwait(false);
            File.Delete(tmp);
        }

        public static async Task RepostPKMAsShowdownAsync(this ISocketMessageChannel channel, IAttachment att)
        {
            if (!EntityDetection.IsSizePlausible(att.Size))
                return;
            var result = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
            if (!result.Success)
                return;

            var pkm = result.Data!;
            await channel.SendPKMAsShowdownSetAsync(pkm).ConfigureAwait(false);
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

        public static async Task SendPKMAsShowdownSetAsync(this ISocketMessageChannel channel, PKM pkm)
        {
            var txt = GetFormattedShowdownText(pkm);
            await channel.SendMessageAsync(txt).ConfigureAwait(false);
        }

        public static string GetFormattedShowdownText(PKM pkm)
        {
            var newShowdown = new List<string>();
            var showdown = ShowdownParsing.GetShowdownText(pkm);
            foreach (var line in showdown.Split('\n'))
                newShowdown.Add($"\n{line}");

            int index = newShowdown.FindIndex(z => z.Contains("Nature"));
            if (pkm.Ball > (int)Ball.None && index != -1)
                newShowdown.Insert(newShowdown.FindIndex(z => z.Contains("Nature")), $"\nBall: {(Ball)pkm.Ball} Ball");

            index = newShowdown.FindIndex(x => x.Contains("Shiny: Yes"));
            if (pkm is PK8 && pkm.IsShiny && index != -1)
            {
                if (pkm.ShinyXor == 0 || pkm.FatefulEncounter)
                    newShowdown[index] = "\nShiny: Square\r";
                else newShowdown[index] = "\nShiny: Star\r";
            }

            var extra = new string[] { $"\nOT: {pkm.OT_Name}", $"\nTID: {pkm.GetDisplayTID()}", $"\nSID: {pkm.GetDisplaySID()}", $"{(pkm.IsEgg ? "\nIsEgg: Yes" : "")}" };
            newShowdown.InsertRange(1, extra);
            return Format.Code(string.Join("", newShowdown).Trim());
        }

        public static List<string> GetListFromString(string str)
        {
            // Extract comma separated list
            return str.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static string StripCodeBlock(string str) => str.Replace("`\n", "").Replace("\n`", "").Replace("`", "").Trim();
    }
}