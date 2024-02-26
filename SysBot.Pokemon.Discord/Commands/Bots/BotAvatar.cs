using Discord;
using Discord.Commands;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public class BotAvatar : ModuleBase<SocketCommandContext>
    {
        [Command("setavatar")]
        [Alias("botavatar", "changeavatar", "sa", "ba")]
        [Summary("Sets the bot's avatar to a specified GIF.")]
        [RequireOwner]
        public async Task SetAvatarAsync()
        {
            if (Context.Message.Attachments.Count == 0)
            {
                await ReplyAsync("Please attach a GIF image to set as the avatar."); // standard (boring) images can be set via dashboard
                return;
            }
            var attachment = Context.Message.Attachments.First();
            if (!attachment.Filename.EndsWith(".gif"))
            {
                await ReplyAsync("Please provide a GIF image.");
                return;
            }

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(attachment.Url);

            using var ms = new MemoryStream(imageBytes);
            var image = new Image(ms);
            await Context.Client.CurrentUser.ModifyAsync(user => user.Avatar = image);

            await ReplyAsync("Avatar updated successfully!");
        }
    }
}
