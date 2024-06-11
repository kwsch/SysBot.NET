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
            var userMessage = Context.Message;

            if (userMessage.Attachments.Count == 0)
            {
                var reply = await ReplyAsync("Please attach a GIF image to set as the avatar."); // standard (boring) images can be set via dashboard
                await Task.Delay(60000);
                await userMessage.DeleteAsync();
                await reply.DeleteAsync();
                return;
            }
            var attachment = userMessage.Attachments.First();
            if (!attachment.Filename.EndsWith(".gif"))
            {
                var reply = await ReplyAsync("Please provide a GIF image.");
                await Task.Delay(60000);
                await userMessage.DeleteAsync();
                await reply.DeleteAsync();
                return;
            }

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(attachment.Url);

            using var ms = new MemoryStream(imageBytes);
            var image = new Image(ms);
            await Context.Client.CurrentUser.ModifyAsync(user => user.Avatar = image);

            var successReply = await ReplyAsync("Avatar updated successfully!");
            await Task.Delay(60000);
            await userMessage.DeleteAsync();
            await successReply.DeleteAsync();
        }
    }
}
