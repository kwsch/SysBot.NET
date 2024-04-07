using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon;

public class StopConditionSettings
{
    private const string StopConditions = nameof(StopConditions);
    public override string ToString() => "Einstellungen der Stoppbedingungen";

    [Category(StopConditions), Description("Hält nur bei Pokémon dieser Art an. Keine Einschränkungen, wenn auf \"None\" eingestellt..")]
    public Species StopOnSpecies { get; set; }

    [Category(StopConditions), Description("Hält nur bei Pokémon mit dieser FormID an. Keine Einschränkungen, wenn leer gelassen.")]
    public int? StopOnForm { get; set; }

    [Category(StopConditions), Description("Nur bei Pokémon der angegebenen Art anhalten.")]
    public Nature TargetNature { get; set; } = Nature.Random;

    [Category(StopConditions), Description("Mindestens akzeptierte IVs im Format HP/Atk/Def/SpA/SpD/Spe. Verwenden Sie \"x\" für nicht geprüfte IVs und \"/\" als Trennzeichen.")]
    public string TargetMinIVs { get; set; } = "";

    [Category(StopConditions), Description("Maximal zulässige IVs im Format HP/Atk/Def/SpA/SpD/Spe. Verwenden Sie \"x\" für nicht geprüfte IVs und \"/\" als Trennzeichen.")]
    public string TargetMaxIVs { get; set; } = "";

    [Category(StopConditions), Description("Wählt die ShinyArt, bei der angehalten werden soll.")]
    public TargetShinyType ShinyTarget { get; set; } = TargetShinyType.DisableOption;

    [Category(StopConditions), Description("Halte nur bei Pokémon an, die eine Markierung haben.")]
    public bool MarkOnly { get; set; }

    [Category(StopConditions), Description("Liste der zu ignorierenden Zeichen, getrennt durch Kommata. Verwenden Sie den vollständigen Namen, z. B. \"Uncommon Mark, Dawn Mark, Prideful Mark\".")]
    public string UnwantedMarks { get; set; } = "";

    [Category(StopConditions), Description("Hält die Aufnahmetaste gedrückt, um einen 30-sekündigen Clip aufzunehmen, wenn ein passendes Pokémon von BegegnungsBot oder Fossilbot gefunden wird.")]
    public bool CaptureVideoClip { get; set; }

    [Category(StopConditions), Description("Zusätzliche Zeit in Millisekunden, die nach einem Treffer gewartet werden soll bevor Fangen für BegegnungsBot oder Fossilbot aktiviert wird.")]
    public int ExtraTimeWaitCaptureVideo { get; set; } = 10000;

    [Category(StopConditions), Description("Wenn auf TRUE gesetzt, werden sowohl ShinyTarget- als auch TargetIVs-Einstellungen abgeglichen. Andernfalls wird entweder nach einer Übereinstimmung mit ShinyTarget oder TargetIVs gesucht.")]
    public bool MatchShinyAndIV { get; set; } = true;

    [Category(StopConditions), Description("Wenn die angegebene Zeichenkette nicht leer ist, wird sie der Ergebnisprotokollmeldung vorangestellt, um Echo-Benachrichtigungen für die von Ihnen angegebene Person zu erhalten. Für Discord verwenden Sie <@userIDnumber>, um zu erwähnen.")]
    public string MatchFoundEchoMention { get; set; } = string.Empty;

    public static bool EncounterFound<T>(T pk, int[] targetminIVs, int[] targetmaxIVs, StopConditionSettings settings, IReadOnlyList<string>? marklist) where T : PKM
    {
        // Match Nature and Species if they were specified.
        if (settings.StopOnSpecies != Species.None && settings.StopOnSpecies != (Species)pk.Species)
            return false;

        if (settings.StopOnForm.HasValue && settings.StopOnForm != pk.Form)
            return false;

        if (settings.TargetNature != Nature.Random && settings.TargetNature != (Nature)pk.Nature)
            return false;

        // Return if it doesn't have a mark, or it has an unwanted mark.
        var unmarked = pk is IRibbonIndex m && !HasMark(m);
        var unwanted = marklist is not null && pk is IRibbonIndex m2 && settings.IsUnwantedMark(GetMarkName(m2), marklist);
        if (settings.MarkOnly && (unmarked || unwanted))
            return false;

        if (settings.ShinyTarget != TargetShinyType.DisableOption)
        {
            bool shinymatch = settings.ShinyTarget switch
            {
                TargetShinyType.AnyShiny => pk.IsShiny,
                TargetShinyType.NonShiny => !pk.IsShiny,
                TargetShinyType.StarOnly => pk.IsShiny && pk.ShinyXor != 0,
                TargetShinyType.SquareOnly => pk.ShinyXor == 0,
                TargetShinyType.DisableOption => true,
                _ => throw new ArgumentException(nameof(TargetShinyType)),
            };

            // If we only needed to match one of the criteria and it shiny match'd, return true.
            // If we needed to match both criteria, and it didn't shiny match, return false.
            if (!settings.MatchShinyAndIV && shinymatch)
                return true;
            if (settings.MatchShinyAndIV && !shinymatch)
                return false;
        }

        // Reorder the speed to be last.
        Span<int> pkIVList = stackalloc int[6];
        pk.GetIVs(pkIVList);
        (pkIVList[5], pkIVList[3], pkIVList[4]) = (pkIVList[3], pkIVList[4], pkIVList[5]);

        for (int i = 0; i < 6; i++)
        {
            if (targetminIVs[i] > pkIVList[i] || targetmaxIVs[i] < pkIVList[i])
                return false;
        }
        return true;
    }

    public static void InitializeTargetIVs(PokeTradeHubConfig config, out int[] min, out int[] max)
    {
        min = ReadTargetIVs(config.StopConditions, true);
        max = ReadTargetIVs(config.StopConditions, false);
    }

    private static int[] ReadTargetIVs(StopConditionSettings settings, bool min)
    {
        int[] targetIVs = new int[6];
        char[] split = ['/'];

        string[] splitIVs = min
            ? settings.TargetMinIVs.Split(split, StringSplitOptions.RemoveEmptyEntries)
            : settings.TargetMaxIVs.Split(split, StringSplitOptions.RemoveEmptyEntries);

        // Only accept up to 6 values.  Fill it in with default values if they don't provide 6.
        // Anything that isn't an integer will be a wild card.
        for (int i = 0; i < 6; i++)
        {
            if (i < splitIVs.Length)
            {
                var str = splitIVs[i];
                if (int.TryParse(str, out var val))
                {
                    targetIVs[i] = val;
                    continue;
                }
            }
            targetIVs[i] = min ? 0 : 31;
        }
        return targetIVs;
    }

    private static bool HasMark(IRibbonIndex pk)
    {
        for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
        {
            if (pk.GetRibbon((int)mark))
                return true;
        }
        return false;
    }

    public static string GetPrintName(PKM pk)
    {
        var set = ShowdownParsing.GetShowdownText(pk);
        if (pk is IRibbonIndex r)
        {
            var rstring = GetMarkName(r);
            if (!string.IsNullOrEmpty(rstring))
                set += $"\nPokémon mit **{GetMarkName(r)}** gefunden!";
        }
        return set;
    }

    public static void ReadUnwantedMarks(StopConditionSettings settings, out IReadOnlyList<string> marks) =>
        marks = settings.UnwantedMarks.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

    public virtual bool IsUnwantedMark(string mark, IReadOnlyList<string> marklist) => marklist.Contains(mark);

    public static string GetMarkName(IRibbonIndex pk)
    {
        for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
        {
            if (pk.GetRibbon((int)mark))
                return RibbonStrings.GetName($"Ribbon{mark}");
        }
        return "";
    }
}

public enum TargetShinyType
{
    DisableOption,  // Doesn't care
    NonShiny,       // Match nonshiny only
    AnyShiny,       // Match any shiny regardless of type
    StarOnly,       // Match star shiny only
    SquareOnly,     // Match square shiny only
}
