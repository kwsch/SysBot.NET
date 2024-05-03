using PKHeX.Core;
using System;
using System.Text;

namespace SysBot.Pokemon;

public static class PreCorrectShowdown
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
            if (firstLine.Contains("(") && firstLine.Contains(")"))
            {
                int startIndex = firstLine.IndexOf("(") + 1;
                int endIndex = firstLine.IndexOf(")", startIndex);
                if (startIndex > 0 && endIndex > startIndex)
                {
                    speciesName = firstLine.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            else
            {
                // Split the first line by space and take the first part as the species name
                string[] parts = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    speciesName = parts[0].Trim();

                    // Check if the species name contains a form name
                    int formIndex = speciesName.IndexOf('-');
                    if (formIndex > 0)
                    {
                        // Include the form name as part of the species name
                        int endIndex = speciesName.IndexOf(' ', formIndex);
                        if (endIndex < 0)
                        {
                            endIndex = speciesName.Length;
                        }
                        speciesName = speciesName.Substring(0, endIndex).Trim();
                    }
                }
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
                    if (firstLine.Contains("(") && firstLine.Contains(")"))
                    {
                        int startIndex = firstLine.IndexOf("(") + 1;
                        int endIndex = firstLine.IndexOf(")", startIndex);
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            firstLine = firstLine.Substring(0, startIndex) + correctedSpeciesName + firstLine.Substring(endIndex);
                        }
                    }
                    else
                    {
                        firstLine = string.Concat(correctedSpeciesName, firstLine.AsSpan(speciesName.Length));
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

        foreach (var species in GameInfo.Strings.Species)
        {
            if (string.IsNullOrEmpty(species))
                continue;

            int distance = LevenshteinDistance(userSpecies.ToLower(), species.ToLower());
            if (distance < minDistance)
            {
                minDistance = distance;
                closestSpecies = species;
            }
        }

        return minDistance <= 2 ? closestSpecies : null;
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

