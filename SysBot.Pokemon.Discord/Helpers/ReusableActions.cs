using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using Image = System.Drawing.Image;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

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

        public static async Task SendImageAsync(this ISocketMessageChannel channel, Image finalQR, string msg = "")
        {
            const string fn = "tmp.png";
            finalQR.Save(fn, ImageFormat.Png);
            await channel.SendFileAsync(fn, msg).ConfigureAwait(false);
        }

        public static async Task RepostPKMAsShowdownAsync(this ISocketMessageChannel channel, IAttachment att)
        {
            if (!PKX.IsPKM(att.Size))
                return;
            var result = await NetUtil.DownloadPKMAsync(att).ConfigureAwait(false);
            if (!result.Success)
                return;

            var pkm = result.Data!;
            await channel.SendPKMAsShowdownSetAsync(pkm).ConfigureAwait(false);
        }

        public static bool GetIsSudo(this SocketCommandContext Context, PokeTradeHubConfig cfg)
        {
            if (cfg.AllowGlobalSudo && cfg.GlobalSudoList.Contains(Context.User.Id.ToString()))
                return true;
            return Context.GetHasRole(cfg.DiscordRoleSudo);
        }

        public static bool GetIsSudo(this SocketCommandContext Context) => Context.GetIsSudo(SysCordInstance.Self.Hub.Config);

        private const string ALLOW_ALL = "@everyone";

        public static bool GetHasRole(this SocketCommandContext Context, string RequiredRole)
        {
            if (RequiredRole == ALLOW_ALL)
                return true;
            var guild = Context.Guild;
            var role = guild.Roles.FirstOrDefault(x => x.Name == RequiredRole);
            if (role == default)
                return false;

            var igu = (SocketGuildUser)Context.User;
            bool hasRole = igu.Roles.Contains(role);
            return hasRole;
        }

        public static async Task SendPKMAsShowdownSetAsync(this ISocketMessageChannel channel, PKM pkm)
        {
            var txt = GetFormattedShowdownText(pkm);
            await channel.SendMessageAsync(txt).ConfigureAwait(false);
        }

        public static string GetFormattedShowdownText(PKM pkm)
        {
            var showdown = ShowdownSet.GetShowdownText(pkm);
            return Format.Code(showdown);
        }

        public static string StripCodeBlock(string str) => str.Replace("`\n", "").Replace("\n`", "").Replace("`", "").Trim();
    }
}