using PKHeX.Core;
using System;
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

        // Parse the species name from the first line
        if (lines.Length > 0)
        {
            string firstLine = lines[0].Trim();

            // Check if the species name is wrapped in parentheses (nicknamed)
            int startIndex = firstLine.IndexOf('(') + 1;
            int endIndex = firstLine.IndexOf(')');
            if (startIndex > 0 && endIndex > startIndex)
            {
                speciesName = firstLine.Substring(startIndex, endIndex - startIndex).Trim();
            }
            else
            {
                // Split the first line by space and take all parts as the species name
                speciesName = string.Join("", firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        string correctedSpeciesName = GetClosestSpecies(speciesName);

        if (!string.IsNullOrEmpty(correctedSpeciesName) && !string.Equals(speciesName, correctedSpeciesName, StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    // Replace the species name in the first line
                    string firstLine = lines[0];
                    int startIndex = firstLine.IndexOf('(') + 1;
                    int endIndex = firstLine.IndexOf(')');
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        firstLine = firstLine.Substring(0, startIndex) + correctedSpeciesName + firstLine.Substring(endIndex + 1);
                    }
                    else
                    {
                        firstLine = correctedSpeciesName;
                    }
                    correctedContent.AppendLine(firstLine);
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

