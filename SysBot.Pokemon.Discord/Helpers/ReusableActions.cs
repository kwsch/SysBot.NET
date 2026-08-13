using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord;

public static class ReusableActions
{
    /// <summary>
    /// Weak binding to a function that returns a sprite png data stream for a given PKM.
    /// This is used to provide a thumbnail in Discord messages.
    /// </summary>
    public static Func<PKM, MemoryStream>? GetSprite { get; set; }

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

    extension(IInteractionContext context)
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

    public static string GetFormattedShowdownText(PKM pk, LanguageID displayLanguage = LanguageID.English)
    {
        var config = BattleTemplateConfig.Showdown;

        var settings = new BattleTemplateExportSettings(config, displayLanguage);
        var showdown = ShowdownParsing.GetShowdownText(pk, settings);

        return FormatSetCode(showdown);
    }

    // yml looks nicest of all code-languages in Discord code blocks, so we use that instead of plain text.
    private const string CodeLanguage = "yml";
    private const LanguageID Language = LanguageID.English;

    public static string FormatSetCode(string set) => Format.Code(set, CodeLanguage);
    public static string FormatSetCode(IEnumerable<string> lines) => FormatSetCode(string.Join('\n', lines));
    public static string FormatSetCode(ShowdownSet set, LanguageID displayLanguage = Language)
    {
        var config = BattleTemplateConfig.Showdown;
        var settings = new BattleTemplateExportSettings(config, displayLanguage);
        var lines = set.GetSetLines(settings);
        return FormatSetCode(lines);
    }

    public static string FormatSetCode<T>(T trade, LanguageID language = Language) where T : PKM
    {
        var localization = BattleTemplateLocalization.GetLocalization(Language);
        var set = new ShowdownSet(trade, localization);
        return FormatSetCode(set, language);
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

public static class PersonalColorExtensions
{
    private static readonly Dictionary<PersonalColor, Color> Map = new()
    {
        [PersonalColor.Red] = new Color(0xE5, 0x3D, 0x3D),
        [PersonalColor.Blue] = new Color(0x3D, 0x7D, 0xE5),
        [PersonalColor.Yellow] = new Color(0xE5, 0xD3, 0x3D),
        [PersonalColor.Green] = new Color(0x4C, 0xAF, 0x50),
        [PersonalColor.Black] = new Color(0x2C, 0x2C, 0x2C),
        [PersonalColor.Brown] = new Color(0x8D, 0x5B, 0x3D),
        [PersonalColor.Purple] = new Color(0x9B, 0x59, 0xB6),
        [PersonalColor.Gray] = new Color(0x95, 0xA5, 0xA6),
        [PersonalColor.White] = new Color(0xEC, 0xF0, 0xF1),
        [PersonalColor.Pink] = new Color(0xE9, 0x1E, 0x8C),
    };

    extension(PKM pk)
    {
        public Color ToDiscordColor() =>
            Map.TryGetValue((PersonalColor)pk.PersonalInfo.Color, out var c) ? c : Color.Default;
    }

    extension(PersonalColor color)
    {
        public Color ToDiscordColor() =>
            Map.TryGetValue(color, out var c) ? c : Color.Default;
    }
}
