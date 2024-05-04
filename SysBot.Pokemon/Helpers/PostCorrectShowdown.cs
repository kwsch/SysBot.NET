using FuzzySharp;
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
        string levelValue = string.Empty;

        // Parse the species name, form name, ability, nature, and ball from the lines
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("Ability:"))
            {
                abilityName = trimmedLine["Ability:".Length..].Trim();
            }
            else if (trimmedLine.StartsWith("Nature:"))
            {
                natureName = trimmedLine["Nature:".Length..].Trim();
            }
            else if (trimmedLine.StartsWith("Ball:")) 
            {
                ballName = trimmedLine["Ball:".Length..].Trim();
            }
            else if (trimmedLine.StartsWith("Level:"))
            {
                levelValue = trimmedLine["Level:".Length..].Trim();
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
        string correctedBallName = GetLegalBall(speciesIndex, correctedFormName, ballName, gameStrings, data);
        var levelVerifier = new LevelVerifier();
        levelVerifier.Verify(data);

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
            else if (line.StartsWith("Level:"))
            {
                if (!data.Valid)
                    correctedLine = "Level: 99";
                else
                    correctedLine = $"Level: {levelValue}";
            }
            correctedContent.AppendLine(correctedLine);
        }

        return correctedContent.ToString();
    }

    private static string GetClosestFormName(string userFormName, ushort speciesIndex, GameStrings gameStrings)
    {
        var validFormNames = FormConverter.GetFormList((ushort)speciesIndex, gameStrings.types, gameStrings.forms, new List<string>(), EntityContext.Gen9);

        var fuzzyFormName = validFormNames
            .Where(f => !string.IsNullOrEmpty(f))
            .Select(f => (FormName: f, Distance: CalculateFormDistance(userFormName, f, speciesIndex)))
            .OrderBy(f => f.Distance)
            .ThenByDescending(f => f.FormName.Length)
            .FirstOrDefault();

        return fuzzyFormName.Distance <= 2 ? fuzzyFormName.FormName : null;
    }

    private static int CalculateFormDistance(string userFormName, string formName, ushort speciesIndex)
    {
        string showdownFormName = ShowdownParsing.GetShowdownFormName(speciesIndex, formName);
        return Fuzz.Ratio(userFormName, showdownFormName);
    }
    private static string GetClosestSpecies(string userSpecies)
    {
        var gameStrings = GetGameStrings();
        var pkms = gameStrings.specieslist.Select(s => new T { Species = (ushort)Array.IndexOf(gameStrings.specieslist, s) });
        var sortedSpecies = pkms.OrderBySpeciesName(gameStrings.specieslist).Select(p => gameStrings.specieslist[p.Species]);

        var fuzzySpecies = sortedSpecies
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => (Species: s, Distance: Fuzz.Ratio(userSpecies, s)))
            .OrderByDescending(s => s.Distance)
            .FirstOrDefault();

        return fuzzySpecies.Distance >= 80 ? fuzzySpecies.Species : null;
    }

    private static string GetClosestAbility(string userAbility, ushort speciesIndex, GameStrings gameStrings, IPersonalAbility12 personalInfo)
    {
        var abilities = Enumerable.Range(0, personalInfo.AbilityCount)
            .Select(i => gameStrings.abilitylist[personalInfo.GetAbilityAtIndex(i)])
            .Where(a => !string.IsNullOrEmpty(a));

        var fuzzyAbility = abilities
            .Select(a => (Ability: a, Distance: Fuzz.Ratio(userAbility, a)))
            .OrderByDescending(a => a.Distance)
            .FirstOrDefault();

        return fuzzyAbility.Distance >= 80 ? fuzzyAbility.Ability : null;
    }

    private static string GetClosestNature(string userNature, GameStrings gameStrings)
    {
        var fuzzyNature = gameStrings.natures
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => (Nature: n, Distance: Fuzz.Ratio(userNature, n)))
            .OrderByDescending(n => n.Distance)
            .FirstOrDefault();

        return fuzzyNature.Distance >= 80 ? fuzzyNature.Nature : null;
    }

    private static string GetClosestBall(string userBall, GameStrings gameStrings, ulong legalBalls)
    {
        var fuzzyBall = gameStrings.balllist
            .Where(b => !string.IsNullOrWhiteSpace(b) && IsBallPermitted(legalBalls, (byte)Array.IndexOf(gameStrings.itemlist, b)))
            .Select(b => (BallName: b, Distance: Fuzz.PartialRatio(userBall, b)))
            .OrderByDescending(b => b.Distance)
            .FirstOrDefault();

        return fuzzyBall != default ? fuzzyBall.BallName : gameStrings.itemlist[(int)Ball.Poke];
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
}

