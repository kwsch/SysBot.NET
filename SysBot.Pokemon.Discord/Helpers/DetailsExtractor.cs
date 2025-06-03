using Discord;
using Discord.WebSocket;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Discord;

/// <summary>
/// Extracts and formats details from Pokémon data for Discord embed displays.
/// </summary>
/// <typeparam name="T">Type of Pokémon data structure.</typeparam>
public static class DetailsExtractor<T> where T : PKM, new()
{
    /// <summary>
    /// Adds additional text to the embed as configured in settings.
    /// </summary>
    /// <param name="embedBuilder">Discord embed builder to modify.</param>
    public static void AddAdditionalText(EmbedBuilder embedBuilder)
    {
        string additionalText = string.Join("\n", SysCordSettings.Settings.AdditionalEmbedText);
        if (!string.IsNullOrEmpty(additionalText))
        {
            embedBuilder.AddField("\u200B", additionalText, inline: false);
        }
    }

    /// <summary>
    /// Adds normal trade information fields to the embed.
    /// </summary>
    /// <param name="embedBuilder">Discord embed builder to modify.</param>
    /// <param name="embedData">Extracted Pokémon data.</param>
    /// <param name="trainerMention">Discord mention for the trainer.</param>
    /// <param name="pk">Pokémon data.</param>
    public static void AddNormalTradeFields(EmbedBuilder embedBuilder, EmbedData embedData, string trainerMention, T pk)
    {
        string leftSideContent = $"**Trainer:** {trainerMention}\n";
        leftSideContent +=
            (pk.Version is GameVersion.SL or GameVersion.VL && SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowTeraType ? $"**Tera Type:** {embedData.TeraType}\n" : "") +
            (pk.Version is GameVersion.SL or GameVersion.VL && SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowScale ? $"**Scale:** {embedData.Scale.Item1} ({embedData.Scale.Item2})\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowLevel ? $"**Level:** {embedData.Level}\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowMetDate ? $"**Met Date:** {embedData.MetDate}\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowAbility ? $"**Ability:** {embedData.Ability}\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowNature ? $"**Nature**: {embedData.Nature}\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowIVs ? $"**IVs**: {embedData.IVsDisplay}\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowLanguage ? $"**Language**: {embedData.Language}\n" : "") +
            (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.ShowEVs && !string.IsNullOrWhiteSpace(embedData.EVsDisplay) ? $"**EVs**: {embedData.EVsDisplay}\n" : "");

        leftSideContent = leftSideContent.TrimEnd('\n');
        embedBuilder.AddField($"**{embedData.SpeciesName}{(string.IsNullOrEmpty(embedData.FormName) ? "" : $"-{embedData.FormName}")} {embedData.SpecialSymbols}**", leftSideContent, inline: true);
        embedBuilder.AddField("\u200B", "\u200B", inline: true);
        embedBuilder.AddField("**Moves:**", embedData.MovesDisplay, inline: true);
    }

    /// <summary>
    /// Adds special trade information fields to the embed.
    /// </summary>
    /// <param name="embedBuilder">Discord embed builder to modify.</param>
    /// <param name="isMysteryEgg">Whether this is a mystery egg trade.</param>
    /// /// <param name="isMysteryMon">Whether this is a mystery trade.</param>
    /// <param name="isSpecialRequest">Whether this is a special request trade.</param>
    /// <param name="isCloneRequest">Whether this is a clone request trade.</param>
    /// <param name="isFixOTRequest">Whether this is a fix OT request trade.</param>
    /// <param name="trainerMention">Discord mention for the trainer.</param>
    public static void AddSpecialTradeFields(EmbedBuilder embedBuilder, bool isMysteryMon, bool isMysteryEgg, bool isSpecialRequest, bool isCloneRequest, bool isFixOTRequest, string trainerMention)
    {
        string specialDescription = $"**Trainer:** {trainerMention}\n" +
                                    (isMysteryMon ? "Mystery Pokémon" : isMysteryEgg ? "Mystery Egg" : isSpecialRequest ? "Special Request" : isCloneRequest ? "Clone Request" : isFixOTRequest ? "FixOT Request" : "Dump Request");
        embedBuilder.AddField("\u200B", specialDescription, inline: false);
    }

    /// <summary>
    /// Adds thumbnails to the embed based on trade type.
    /// </summary>
    /// <param name="embedBuilder">Discord embed builder to modify.</param>
    /// <param name="isCloneRequest">Whether this is a clone request trade.</param>
    /// <param name="isSpecialRequest">Whether this is a special request trade.</param>
    /// <param name="heldItemUrl">URL for the held item image.</param>
    public static void AddThumbnails(EmbedBuilder embedBuilder, bool isCloneRequest, bool isSpecialRequest, string heldItemUrl)
    {
        if (isCloneRequest || isSpecialRequest)
        {
            embedBuilder.WithThumbnailUrl("https://raw.githubusercontent.com/Havokx89/sprites/main/profoak.png");
        }
        else if (!string.IsNullOrEmpty(heldItemUrl))
        {
            embedBuilder.WithThumbnailUrl(heldItemUrl);
        }
    }

    /// <summary>
    /// Extracts detailed information from a Pokémon for display.
    /// </summary>
    /// <param name="pk">Pokémon data.</param>
    /// <param name="user">Discord user initiating the trade.</param>
    /// <param name="isMysteryEgg">Whether this is a mystery egg trade.</param>
    /// /// <param name="isMysteryMon">Whether this is a mystery trade.</param>
    /// <param name="isCloneRequest">Whether this is a clone request trade.</param>
    /// <param name="isDumpRequest">Whether this is a dump request trade.</param>
    /// <param name="isFixOTRequest">Whether this is a fix OT request trade.</param>
    /// <param name="isSpecialRequest">Whether this is a special request trade.</param>
    /// <param name="isBatchTrade">Whether this is part of a batch trade.</param>
    /// <param name="batchTradeNumber">The number of this trade in the batch sequence.</param>
    /// <param name="totalBatchTrades">Total number of trades in the batch.</param>
    /// <returns>Structured Pokémon data for embed display.</returns>
    public static EmbedData ExtractPokemonDetails(T pk, SocketUser user, bool isMysteryMon, bool isMysteryEgg, bool isCloneRequest, bool isDumpRequest, bool isFixOTRequest, bool isSpecialRequest, bool isBatchTrade, int batchTradeNumber, int totalBatchTrades)
    {
        string langCode = ((LanguageID)pk.Language).GetLanguageCode();
        GameStrings strings = GameInfo.GetStrings(langCode);

        var originalLanguage = GameInfo.CurrentLanguage;
        GameInfo.CurrentLanguage = langCode;

        var embedData = new EmbedData
        {
            Moves = GetMoveNames(pk, strings),
            Level = pk.CurrentLevel
        };

        int languageId = pk.Language;
        string languageDisplay = GetLanguageDisplay(pk);
        embedData.Language = languageDisplay;

        if (pk is PK9 pk9)
        {
            embedData.TeraType = GetTeraTypeString(pk9);
            embedData.Scale = GetScaleDetails(pk9);
        }

        embedData.Ability = GetAbilityName(pk, strings);
        embedData.Nature = GetNatureName(pk, strings);
        embedData.SpeciesName = strings.Species[pk.Species];
        embedData.SpecialSymbols = GetSpecialSymbols(pk);
        embedData.FormName = ShowdownParsing.GetStringFromForm(pk.Form, strings, pk.Species, pk.Context);
        embedData.HeldItem = strings.itemlist[pk.HeldItem];
        embedData.Ball = strings.balllist[pk.Ball];

        int[] ivs = pk.IVs;
        string ivsDisplay;
        if (ivs.All(iv => iv == 31))
        {
            ivsDisplay = "6IV";
        }
        else
        {
            ivsDisplay = string.Join("/", new[]
            {
                ivs[0].ToString(),
                ivs[1].ToString(),
                ivs[2].ToString(),
                ivs[4].ToString(),
                ivs[5].ToString(),
                ivs[3].ToString()
            });
        }
        embedData.IVsDisplay = ivsDisplay;

        int[] evs = GetEVs(pk);
        embedData.EVsDisplay = string.Join(" / ", new[] {
            (evs[0] != 0 ? $"{evs[0]} HP" : ""),
            (evs[1] != 0 ? $"{evs[1]} Atk" : ""),
            (evs[2] != 0 ? $"{evs[2]} Def" : ""),
            (evs[4] != 0 ? $"{evs[4]} SpA" : ""),
            (evs[5] != 0 ? $"{evs[5]} SpD" : ""),
            (evs[3] != 0 ? $"{evs[3]} Spe" : "")
        }.Where(s => !string.IsNullOrEmpty(s)));
        embedData.MetDate = pk.MetDate.ToString();
        embedData.MovesDisplay = string.Join("\n", embedData.Moves);
        embedData.PokemonDisplayName = pk.IsNicknamed ? pk.Nickname : embedData.SpeciesName;

        embedData.TradeTitle = GetTradeTitle(isMysteryEgg, isCloneRequest, isDumpRequest, isFixOTRequest, isSpecialRequest, isBatchTrade, batchTradeNumber, embedData.PokemonDisplayName, pk.IsShiny);
        embedData.AuthorName = GetAuthorName(user.Username, embedData.TradeTitle, isMysteryEgg, isFixOTRequest, isCloneRequest, isDumpRequest, isSpecialRequest, isBatchTrade, embedData.PokemonDisplayName, pk.IsShiny);

        GameInfo.CurrentLanguage = originalLanguage;

        return embedData;
    }

    /// <summary>
    /// Gets user details for display.
    /// </summary>
    /// <param name="totalTradeCount">Total number of trades for this user.</param>
    /// <param name="tradeDetails">Trade code details if available.</param>
    /// <returns>Formatted user details string.</returns>
    public static string GetUserDetails(int totalTradeCount, TradeCodeStorage.TradeCodeDetails? tradeDetails)
    {
        string userDetailsText = "";
        if (totalTradeCount > 0)
        {
            userDetailsText = $"Trades: {totalTradeCount}";
        }
        if (SysCord<T>.Runner.Config.Trade.TradeConfiguration.StoreTradeCodes && tradeDetails != null)
        {
            if (!string.IsNullOrEmpty(tradeDetails?.OT))
            {
                userDetailsText += $" | OT: {tradeDetails?.OT}";
            }
            if (tradeDetails?.TID != null)
            {
                userDetailsText += $" | TID: {tradeDetails?.TID}";
            }
            if (tradeDetails?.TID != null)
            {
                userDetailsText += $" | SID: {tradeDetails?.SID}";
            }
        }
        return userDetailsText;
    }

    private static string GetLanguageDisplay(T pk)
    {
        int safeLanguage = (int)Language.GetSafeLanguage(pk.Generation, (LanguageID)pk.Language, (GameVersion)pk.Version);

        string languageName = "Unknown";
        var languageList = GameInfo.LanguageDataSource(pk.Format);
        var languageEntry = languageList.FirstOrDefault(l => l.Value == pk.Language);

        if (languageEntry != null)
        {
            languageName = languageEntry.Text;
        }
        else
        {
            languageName = ((LanguageID)pk.Language).GetLanguageCode();
        }

        if (safeLanguage != pk.Language)
        {
            string safeLanguageName = languageList.FirstOrDefault(l => l.Value == safeLanguage)?.Text ?? ((LanguageID)safeLanguage).GetLanguageCode();
            return $"{languageName} (Safe: {safeLanguageName})";
        }

        return languageName;
    }

    private static string GetAbilityName(T pk, GameStrings strings)
    {
        return strings.abilitylist[pk.Ability];
    }

    private static string GetAuthorName(string username, string tradeTitle, bool isMysteryEgg, bool isFixOTRequest, bool isCloneRequest, bool isDumpRequest, bool isSpecialRequest, bool isBatchTrade, string pokemonDisplayName, bool isShiny)
    {
        string isPkmShiny = isShiny ? "Shiny " : "";
        return isMysteryEgg || isFixOTRequest || isCloneRequest || isDumpRequest || isSpecialRequest || isBatchTrade ?
               $"{username}'s {tradeTitle}" :
               $"{username}'s {isPkmShiny}{pokemonDisplayName}";
    }

    private static int[] GetEVs(T pk)
    {
        int[] evs = new int[6];
        pk.GetEVs(evs);
        return evs;
    }

    private static List<string> GetMoveNames(T pk, GameStrings strings)
    {
        ushort[] moves = new ushort[4];
        pk.GetMoves(moves.AsSpan());
        List<int> movePPs = [pk.Move1_PP, pk.Move2_PP, pk.Move3_PP, pk.Move4_PP];
        var moveNames = new List<string>();

        var typeEmojis = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.CustomTypeEmojis
            .Where(e => !string.IsNullOrEmpty(e.EmojiCode))
            .ToDictionary(e => (PKHeX.Core.MoveType)e.MoveType, e => $"{e.EmojiCode}");

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i] == 0) continue;
            string moveName = strings.movelist[moves[i]];
            byte moveTypeId = MoveInfo.GetType(moves[i], default);
            PKHeX.Core.MoveType moveType = (PKHeX.Core.MoveType)moveTypeId;
            string formattedMove = $"{moveName} ({movePPs[i]}pp)";
            if (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MoveTypeEmojis && typeEmojis.TryGetValue(moveType, out var moveEmoji))
            {
                formattedMove = $"{moveEmoji} {formattedMove}";
            }
            moveNames.Add($"\u200B{formattedMove}");
        }

        return moveNames;
    }

    private static string GetNatureName(T pk, GameStrings strings)
    {
        return strings.natures[(int)pk.Nature];
    }

    private static (string, byte) GetScaleDetails(PK9 pk9)
    {
        string scaleText = $"{PokeSizeDetailedUtil.GetSizeRating(pk9.Scale)}";
        byte scaleNumber = pk9.Scale;
        return (scaleText, scaleNumber);
    }

    private static string GetSpecialSymbols(T pk)
    {
        string alphaMarkSymbol = string.Empty;
        string mightyMarkSymbol = string.Empty;
        string markTitle = string.Empty;
        if (pk is IRibbonSetMark9 ribbonSetMark)
        {
            alphaMarkSymbol = ribbonSetMark.RibbonMarkAlpha ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.AlphaMarkEmoji.EmojiString : string.Empty;
            mightyMarkSymbol = ribbonSetMark.RibbonMarkMightiest ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MightiestMarkEmoji.EmojiString : string.Empty;
        }
        if (pk is IRibbonIndex ribbonIndex)
        {
            TradeExtensions<T>.HasMark(ribbonIndex, out RibbonIndex result, out markTitle);
        }
        string alphaSymbol = (pk is IAlpha alpha && alpha.IsAlpha) ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.AlphaPLAEmoji.EmojiString : string.Empty;
        string shinySymbol = pk.ShinyXor == 0 ? "◼ " : pk.IsShiny ? "★ " : string.Empty;
        string genderSymbol = GameInfo.GenderSymbolASCII[pk.Gender];
        string maleEmojiString = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MaleEmoji.EmojiString;
        string femaleEmojiString = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.FemaleEmoji.EmojiString;
        string displayGender = genderSymbol switch
        {
            "M" => !string.IsNullOrEmpty(maleEmojiString) ? maleEmojiString : "(M) ",
            "F" => !string.IsNullOrEmpty(femaleEmojiString) ? femaleEmojiString : "(F) ",
            _ => ""
        };
        string mysteryGiftEmoji = pk.FatefulEncounter ? SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.MysteryGiftEmoji.EmojiString : "";

        return shinySymbol + alphaSymbol + mightyMarkSymbol + alphaMarkSymbol + mysteryGiftEmoji + displayGender + (!string.IsNullOrEmpty(markTitle) ? $"{markTitle} " : "");
    }

    private static string GetTeraTypeString(PK9 pk9)
    {
        var isStellar = pk9.TeraTypeOverride == (MoveType)TeraTypeUtil.Stellar || (int)pk9.TeraType == 99;
        var teraType = isStellar ? TradeSettings.MoveType.Stellar : (TradeSettings.MoveType)pk9.TeraType;

        if (SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.UseTeraEmojis)
        {
            var emojiInfo = SysCord<T>.Runner.Config.Trade.TradeEmbedSettings.TeraTypeEmojis.Find(e => e.MoveType == teraType);
            if (emojiInfo != null && !string.IsNullOrEmpty(emojiInfo.EmojiCode))
            {
                return emojiInfo.EmojiCode;
            }
        }

        return teraType.ToString();
    }

    private static string GetTradeTitle(bool isMysteryEgg, bool isCloneRequest, bool isDumpRequest, bool isFixOTRequest, bool isSpecialRequest, bool isBatchTrade, int batchTradeNumber, string pokemonDisplayName, bool isShiny)
    {
        string shinyEmoji = isShiny ? "✨ " : "";
        return isMysteryEgg ? "✨ Shiny Mystery Egg ✨" :
               isBatchTrade ? $"Batch Trade #{batchTradeNumber} - {shinyEmoji}{pokemonDisplayName}" :
               isFixOTRequest ? "FixOT Request" :
               isSpecialRequest ? "Special Request" :
               isCloneRequest ? "Clone Pod Activated!" :
               isDumpRequest ? "Pokémon Dump" :
               "";
    }
}

/// <summary>
/// Container for Pokémon data formatted for Discord embed display.
/// </summary>
public class EmbedData
{
    /// <summary>Pokémon ability name.</summary>
    public string? Ability { get; set; }

    /// <summary>Author name for the embed.</summary>
    public string? AuthorName { get; set; }

    /// <summary>Poké Ball name.</summary>
    public string? Ball { get; set; }

    /// <summary>URL for embed image.</summary>
    public string? EmbedImageUrl { get; set; }

    /// <summary>Formatted EVs display string.</summary>
    public string? EVsDisplay { get; set; }

    /// <summary>Form name.</summary>
    public string? FormName { get; set; }

    /// <summary>Held item name.</summary>
    public string? HeldItem { get; set; }

    /// <summary>URL for held item image.</summary>
    public string? HeldItemUrl { get; set; }

    /// <summary>Whether the image is from a local file.</summary>
    public bool IsLocalFile { get; set; }

    /// <summary>Formatted IVs display string.</summary>
    public string? IVsDisplay { get; set; }

    /// <summary>Pokémon language.</summary>
    public string? Language { get; set; }

    /// <summary>Pokémon level.</summary>
    public int Level { get; set; }

    /// <summary>Met date.</summary>
    public string? MetDate { get; set; }

    /// <summary>List of move names.</summary>
    public List<string>? Moves { get; set; }

    /// <summary>Formatted moves display string.</summary>
    public string? MovesDisplay { get; set; }

    /// <summary>Nature name.</summary>
    public string? Nature { get; set; }

    /// <summary>Displayed Pokémon name (nickname or species).</summary>
    public string? PokemonDisplayName { get; set; }

    /// <summary>Size scale rating and number.</summary>
    public (string, byte) Scale { get; set; }

    /// <summary>Special symbol indicators (shiny, gender, etc.).</summary>
    public string? SpecialSymbols { get; set; }

    /// <summary>Species name.</summary>
    public string? SpeciesName { get; set; }

    /// <summary>Tera type for PLA/SV.</summary>
    public string? TeraType { get; set; }

    /// <summary>Trade title for the embed.</summary>
    public string? TradeTitle { get; set; }
}
