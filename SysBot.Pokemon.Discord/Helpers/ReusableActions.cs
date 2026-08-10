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
    public static string GetModuleName(string name)
    {
        name = name.Replace("Module", "");
        // Trim off any generic type parameters (e.g., `1, `2) from the name for comparison purposes.
        var gen = name.IndexOf('`');
        if (gen != -1)
            name = name[..gen];
        return name;
    }


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
        public FileAttachment ToFileAttachment()
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

        return FormatSetCode(showdown);
    }

    // yml looks nicest of all code-languages in Discord code blocks, so we use that instead of plain text.
    private const string CodeLanguage = "yml";
    private const LanguageID Language = LanguageID.English;

    public static string FormatSetCode(string set) => Format.Code(set, CodeLanguage);
    public static string FormatSetCode(IEnumerable<string> lines) => FormatSetCode(string.Join('\n', lines));
    public static string FormatSetCode(ShowdownSet set, LanguageID language = Language)
    {
        var config = BattleTemplateConfig.Showdown;
        var settings = new BattleTemplateExportSettings(config, language);
        var lines = set.GetSetLines(settings);
        return FormatSetCode(lines);
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
