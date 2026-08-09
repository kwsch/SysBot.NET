using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public static class ReusableActions
{
    extension(IMessageChannel channel)
    {
        public async Task SendFileAsync(PKM pkm, string message = "", Embed? embed = null)
        {
            var attach = pkm.ToFileAttachment();
            await channel.SendFileAsync(attach, message, embed: embed).ConfigureAwait(false);
        }
    }

    extension(SocketInteractionContext context)
    {
        public async Task SendFileAsync(PKM pk, string message = "", Embed? embed = null)
            => await context.SendFileAsync([pk], message, embed).ConfigureAwait(false);
        public async Task SendFileAsync(IEnumerable<PKM> list, string message = "", Embed? embed = null)
        {
            var attach = list.Select(ToFileAttachment);
            var interaction = context.Interaction;
            var task = interaction.HasResponded
                ? interaction.FollowupWithFilesAsync(attach, message, embed: embed)
                : interaction.RespondWithFilesAsync(attach, message, embed: embed);
            await task.ConfigureAwait(false);
        }

        public async Task SendFilePrivatelyAsync(PKM pk, string message = "", Embed? embed = null)
            => await context.SendFilePrivatelyAsync([pk], message, embed).ConfigureAwait(false);

        public async Task SendFilePrivatelyAsync(IEnumerable<PKM> list, string message = "", Embed? embed = null)
        {
            var user = context.Interaction.User;
            var attach = list.Select(ToFileAttachment);
            await user.SendFilesAsync(attach, message, embed: embed).ConfigureAwait(false);
        }

    }

    extension(PKM pk)
    {
        private FileAttachment ToFileAttachment()
        {
            Span<byte> data = stackalloc byte[pk.SIZE_PARTY];
            pk.WriteDecryptedDataParty(data);
            var result = data.ToArray();

            // No need to save it to the host disk, can just send it directly from memory.
            var stream = new MemoryStream(result);
            var fileName = PathUtil.CleanFileName(pk.FileName);
            return new FileAttachment(stream, fileName);
        }
    }

    public static string GetFormattedShowdownText(PKM pk, LanguageID language = LanguageID.English)
    {
        var config = BattleTemplateConfig.Showdown;

        var settings = new BattleTemplateExportSettings(config, language);
        var showdown = ShowdownParsing.GetShowdownText(pk, settings);

        return Format.Code(showdown);
    }

    /// <summary>
    /// Removes the Discord code formatting.
    /// </summary>
    public static string StripCodeBlock(string message) => message
        .Replace("`\n", "")
        .Replace("\n`", "")
        .Replace("`", "")
        .Trim();
}
