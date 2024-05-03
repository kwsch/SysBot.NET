using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon;

public static class PreCorrectShowdown<T> where T : PKM, new()
{
    public static string PerformSpellCheck(string content)
    {
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder correctedContent = new StringBuilder();

        string speciesName = string.Empty;
        string formName = string.Empty;

        // Parse the species name and form name from the first line
        if (lines.Length > 0)
        {
            string firstLine = lines[0].Trim();
            string[] parts = firstLine.Split('-');
            speciesName = parts[0].Trim();
            formName = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        }

        var gameStrings = GetGameStrings();
        string correctedSpeciesName = GetClosestSpecies(speciesName);
        ushort speciesIndex = (ushort)Array.IndexOf(gameStrings.specieslist, correctedSpeciesName);
        string correctedFormName = GetClosestFormName(formName, speciesIndex, gameStrings);

        if (!string.IsNullOrEmpty(correctedSpeciesName) && !string.Equals(speciesName, correctedSpeciesName, StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    // Replace the species name and form name in the first line
                    if (!string.IsNullOrEmpty(correctedFormName) && !string.Equals(formName, correctedFormName, StringComparison.OrdinalIgnoreCase))
                    {
                        correctedContent.AppendLine($"{correctedSpeciesName}-{correctedFormName}");
                    }
                    else
                    {
                        correctedContent.AppendLine($"{correctedSpeciesName}{(string.IsNullOrEmpty(formName) ? string.Empty : $"-{formName}")}");
                    }
                }
                else
                {
                    correctedContent.AppendLine(lines[i]);
                }
            }
        }
        else
        {
            correctedContent.Append(content);
        }

        return correctedContent.ToString();
    }

    private static string GetClosestFormName(string userFormName, ushort speciesIndex, GameStrings gameStrings)
    {
        int minDistance = int.MaxValue;
        string closestFormName = null;

        var validFormNames = FormConverter.GetFormList((ushort)speciesIndex, gameStrings.types, gameStrings.forms, new List<string>(), EntityContext.Gen9);

        var userFormParts = userFormName.Split('-');

        foreach (var formName in validFormNames)
        {
            if (string.IsNullOrEmpty(formName))
                continue;

            int distance = CalculateFormDistance(userFormName, formName, speciesIndex);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestFormName = formName;
            }
            else if (distance == minDistance)
            {
                // If the distances are equal, keep the longest form name
                if (formName.Length > closestFormName.Length)
                {
                    closestFormName = formName;
                }
            }
        }

        return minDistance <= 2 ? closestFormName : null;
    }

    private static int CalculateFormDistance(string userFormName, string formName, ushort speciesIndex)
    {
        int distance;
        string showdownFormName = ShowdownParsing.GetShowdownFormName(speciesIndex, formName);
        distance = LevenshteinDistance(userFormName.ToLowerInvariant(), showdownFormName.ToLowerInvariant());

        // Special case for multi-part form names
        if (distance > 2 && formName.Contains('-'))
        {
            string[] formParts = formName.Split('-');
            string reconstructedFormName = string.Join("", formParts.Select((part, index) => index == 0 ? part : $"-{part}"));
            distance = LevenshteinDistance(userFormName.ToLowerInvariant(), reconstructedFormName.ToLowerInvariant());
        }

        return distance;
    }

    private static string GetClosestSpecies(string userSpecies)
    {
        int minDistance = int.MaxValue;
        string closestSpecies = null;

        var gameStrings = GetGameStrings();
        var pkms = gameStrings.specieslist.Select(s => new T { Species = (ushort)Array.IndexOf(gameStrings.specieslist, s) });
        var sortedSpecies = pkms.OrderBySpeciesName(gameStrings.specieslist).Select(p => gameStrings.specieslist[p.Species]);

        // Remove spaces from the user input
        userSpecies = userSpecies.Replace(" ", "");

        foreach (var species in sortedSpecies)
        {
            if (string.IsNullOrEmpty(species))
                continue;

            // Remove spaces from the species name
            string speciesName = species.Replace(" ", "");

            if (speciesName.Equals(userSpecies, StringComparison.OrdinalIgnoreCase))
            {
                return species;
            }

            int distance = LevenshteinDistance(userSpecies.ToLowerInvariant(), speciesName.ToLowerInvariant());

            if (distance < minDistance)
            {
                minDistance = distance;
                closestSpecies = species;
            }
        }

        return minDistance <= 2 ? closestSpecies : null;
    }

    private static GameStrings GetGameStrings()
    {
        if (typeof(T) == typeof(PK8))
            return GameInfo.GetStrings(GetLanguageIndex(GameVersion.SWSH));
        if (typeof(T) == typeof(PB8))
            return GameInfo.GetStrings(GetLanguageIndex(GameVersion.BDSP));
        if (typeof(T) == typeof(PA8))
            return GameInfo.GetStrings(GetLanguageIndex(GameVersion.PLA));
        if (typeof(T) == typeof(PK9))
            return GameInfo.GetStrings(GetLanguageIndex(GameVersion.SV));
        if (typeof(T) == typeof(PB7))
            return GameInfo.GetStrings(GetLanguageIndex(GameVersion.GE));

        throw new ArgumentException("Type does not have recognized game strings.", typeof(T).Name);
    }

    private static int GetLanguageIndex(GameVersion version)
    {
        var language = GameLanguage.DefaultLanguage;
        return GameLanguage.GetLanguageIndex(language);
    }

    public static int LevenshteinDistance(string s, string t)
    {
        s = s.Replace("-", "");
        t = t.Replace("-", "");

        string[] sParts = s.Split(' ');
        string[] tParts = t.Split(' ');

        int distance = 0;

        for (int i = 0; i < Math.Max(sParts.Length, tParts.Length); i++)
        {
            if (i >= sParts.Length)
            {
                distance += tParts[i].Length;
            }
            else if (i >= tParts.Length)
            {
                distance += sParts[i].Length;
            }
            else
            {
                distance += CalculateLevenshteinDistance(sParts[i], tParts[i]);
            }
        }

        return distance;
    }

    private static int CalculateLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0)
            return m;

        if (m == 0)
            return n;

        for (int i = 0; i <= n; d[i, 0] = i++) ;
        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                int min1 = d[i - 1, j] + 1;
                int min2 = d[i, j - 1] + 1;
                int min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }
        }

        return d[n, m];
    }
}

