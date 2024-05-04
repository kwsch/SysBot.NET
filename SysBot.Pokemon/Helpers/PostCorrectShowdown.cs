using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PKHeX.Core.BallUseLegality;

namespace SysBot.Pokemon;

public static class PostCorrectShowdown<T> where T : PKM, new()
{
    public static string PerformSpellCheck(string content, LegalityAnalysis data)
    {
        string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder correctedContent = new StringBuilder();

        string speciesName = string.Empty;
        string formName = string.Empty;
        string abilityName = string.Empty;
        string natureName = string.Empty;
        string ballName = string.Empty; 

        // Parse the species name, form name, ability, nature, and ball from the lines
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("Ability:"))
            {
                abilityName = trimmedLine.Substring("Ability:".Length).Trim();
            }
            else if (trimmedLine.StartsWith("Nature:"))
            {
                natureName = trimmedLine.Substring("Nature:".Length).Trim();
            }
            else if (trimmedLine.StartsWith("Ball:")) 
            {
                ballName = trimmedLine.Substring("Ball:".Length).Trim();
            }
            else if (speciesName == string.Empty)
            {
                string[] parts = trimmedLine.Split('-');
                speciesName = parts[0].Trim();
                formName = parts.Length > 1 ? parts[1].Trim() : string.Empty;
            }
        }

        var gameStrings = GetGameStrings();
        string correctedSpeciesName = GetClosestSpecies(speciesName);
        ushort speciesIndex = (ushort)Array.IndexOf(gameStrings.specieslist, correctedSpeciesName);
        string correctedFormName = GetClosestFormName(formName, speciesIndex, gameStrings);
        string correctedAbilityName = GetClosestAbility(abilityName, speciesIndex, gameStrings, GetPersonalInfo(speciesIndex));
        string correctedNatureName = GetClosestNature(natureName, gameStrings);
        string correctedBallName = GetLegalBall(speciesIndex, correctedFormName, ballName, gameStrings, data); // Pass the parsed ballName

        foreach (string line in lines)
        {
            string correctedLine = line;

            if (line.StartsWith(speciesName))
            {
                correctedLine = $"{correctedSpeciesName}{(string.IsNullOrEmpty(correctedFormName) ? string.Empty : $"-{correctedFormName}")}";
            }
            else if (line.StartsWith("Ability:"))
            {
                correctedLine = $"Ability: {correctedAbilityName}";
            }
            else if (line.StartsWith("Nature:"))
            {
                correctedLine = $"Nature: {correctedNatureName}";
            }
            else if (line.StartsWith("Ball:")) 
            {
                correctedLine = $"Ball: {correctedBallName}";
            }

            correctedContent.AppendLine(correctedLine);
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

    private static string GetClosestAbility(string userAbility, ushort speciesIndex, GameStrings gameStrings, IPersonalAbility12 personalInfo)
    {
        int minDistance = int.MaxValue;
        string closestAbility = null;

        for (int abilityIndex = 0; abilityIndex < personalInfo.AbilityCount; abilityIndex++)
        {
            int abilityId = personalInfo.GetAbilityAtIndex(abilityIndex);
            string ability = gameStrings.abilitylist[abilityId];

            if (string.IsNullOrEmpty(ability))
                continue;

            int distance = LevenshteinDistance(userAbility.ToLowerInvariant(), ability.ToLowerInvariant());

            if (distance < minDistance)
            {
                minDistance = distance;
                closestAbility = ability;
            }
        }

        return minDistance <= 2 ? closestAbility : null;
    }

    private static string GetClosestNature(string userNature, GameStrings gameStrings)
    {
        int minDistance = int.MaxValue;
        string closestNature = null;

        foreach (var nature in gameStrings.natures)
        {
            if (string.IsNullOrEmpty(nature))
                continue;

            int distance = LevenshteinDistance(userNature.ToLowerInvariant(), nature.ToLowerInvariant());

            if (distance < minDistance)
            {
                minDistance = distance;
                closestNature = nature;
            }
        }

        return minDistance <= 2 ? closestNature : null;
    }

    private static string GetLegalBall(ushort speciesIndex, string formName, string ballName, GameStrings gameStrings, LegalityAnalysis data)
    {
        var ballVerifier = new BallVerifier();
        ballVerifier.Verify(data);

        if (data.Valid)
            return ballName;

        var legalBalls = GetWildBalls(data.Info.Generation, data.EncounterMatch.Version);
        var closestBall = GetClosestBall(ballName, gameStrings, legalBalls);

        return closestBall ?? gameStrings.itemlist[(int)Ball.Poke];
    }

    private static string GetClosestBall(string userBall, GameStrings gameStrings, ulong legalBalls)
    {
        int minDistance = int.MaxValue;
        string closestBall = null;

        foreach (string ballName in gameStrings.balllist)
        {
            if (string.IsNullOrWhiteSpace(ballName))
                continue;

            int ballIndex = Array.IndexOf(gameStrings.itemlist, ballName);
            if (ballIndex >= 0 && IsBallPermitted(legalBalls, (byte)ballIndex))
            {
                string[] userBallWords = userBall.ToLowerInvariant().Split(' ');
                string[] ballNameWords = ballName.ToLowerInvariant().Split(' ');

                int distance = 0;
                foreach (string userBallWord in userBallWords)
                {
                    int minWordDistance = int.MaxValue;
                    foreach (string ballNameWord in ballNameWords)
                    {
                        int wordDistance = LevenshteinDistanceBall(userBallWord, ballNameWord);
                        if (wordDistance < minWordDistance)
                            minWordDistance = wordDistance;
                    }
                    distance += minWordDistance;
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestBall = ballName;
                }
            }
        }

        return closestBall ?? gameStrings.itemlist[(int)Ball.Poke];
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

    private static IPersonalAbility12 GetPersonalInfo(ushort speciesIndex)
    {
        if (typeof(T) == typeof(PK8))
            return PersonalTable.SWSH.GetFormEntry(speciesIndex, 0);
        if (typeof(T) == typeof(PB8))
            return PersonalTable.BDSP.GetFormEntry(speciesIndex, 0);
        if (typeof(T) == typeof(PA8))
            return PersonalTable.LA.GetFormEntry(speciesIndex, 0);
        if (typeof(T) == typeof(PK9))
            return PersonalTable.SV.GetFormEntry(speciesIndex, 0);
        if (typeof(T) == typeof(PB7))
            return PersonalTable.GG.GetFormEntry(speciesIndex, 0);

        throw new ArgumentException("Type does not have a recognized personal table.", typeof(T).Name);
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

    private static int LevenshteinDistanceBall(string s, string t)
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

