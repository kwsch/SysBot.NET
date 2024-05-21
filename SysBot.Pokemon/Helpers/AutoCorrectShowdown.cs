using FuzzySharp;
using System.Buffers;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using SysBot.Base;
using System.Text.RegularExpressions;
using static PKHeX.Core.LearnMethod;
using static PKHeX.Core.RibbonIndex;
using System.Threading.Tasks;
using System.Text;

namespace SysBot.Pokemon;

public static class AutoCorrectShowdown<T> where T : PKM, new()
{
    private static readonly char[] separator = new[] { '\r', '\n' };

    public static async Task<string> PerformAutoCorrect(string content, PKM originalPk, LegalityAnalysis originalLa)
    {
        var autoCorrectConfig = new TradeSettings.AutoCorrectShowdownCategory();
        if (!autoCorrectConfig.EnableAutoCorrect)
            return content;

        string[] lines = content.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var gameStrings = GetGameStrings();
        var generation = GetGeneration();
        var itemlist = gameStrings.itemlist;

        (string speciesName, string formName, string gender, string heldItem, string nickname) = ParseSpeciesLine(lines[0]);

        string correctedSpeciesName = autoCorrectConfig.AutoCorrectSpeciesAndForm ? await GetClosestSpecies(speciesName).ConfigureAwait(false) ?? speciesName : speciesName;
        ushort speciesIndex = (ushort)Array.IndexOf(gameStrings.specieslist, correctedSpeciesName);
        string[] formNames = FormConverter.GetFormList(speciesIndex, gameStrings.types, gameStrings.forms, new List<string>(), generation);
        string correctedFormName = autoCorrectConfig.AutoCorrectSpeciesAndForm ? await GetClosestFormName(formName, formNames).ConfigureAwait(false) ?? formName : formName;

        PKM pk = originalPk.Clone();
        LegalityAnalysis la = originalLa;

        bool speciesOrFormCorrected = correctedSpeciesName != speciesName || correctedFormName != formName;
        if (speciesOrFormCorrected)
        {
            pk.Species = speciesIndex;
            var personalFormInfo = await Task.Run(() => GetPersonalFormInfo(speciesIndex)).ConfigureAwait(false);
            pk.Form = correctedFormName != null ? (byte)personalFormInfo.FormIndex(speciesIndex, (byte)Array.IndexOf(formNames, correctedFormName)) : (byte)0;
            la = new LegalityAnalysis(pk);
        }

        (string abilityName, string natureName, string ballName, string levelValue) = ParseLines(lines);

        var personalAbilityInfoTask = Task.Run(() => GetPersonalInfo(speciesIndex));
        var closestAbilityTask = GetClosestAbility(abilityName, speciesIndex, gameStrings, await personalAbilityInfoTask);
        var closestNatureTask = GetClosestNature(natureName, gameStrings);
        var legalBallTask = GetLegalBall(speciesIndex, correctedFormName, ballName, gameStrings, pk);

        string correctedAbilityName = autoCorrectConfig.AutoCorrectAbility ? await closestAbilityTask : abilityName;
        string correctedNatureName = autoCorrectConfig.AutoCorrectNature ? await closestNatureTask : natureName;
        string correctedBallName = autoCorrectConfig.AutoCorrectBall ? await legalBallTask : ballName;

        var levelVerifier = new LevelVerifier();
        if (autoCorrectConfig.AutoCorrectLevel)
            levelVerifier.Verify(la);
        if (autoCorrectConfig.AutoCorrectGender)
            gender = await ValidateGender(pk, gender, speciesName);

        if (autoCorrectConfig.AutoCorrectMovesLearnset)
            ValidateMoves(lines, pk, la, gameStrings, correctedSpeciesName, correctedFormName);

        if (!ValidateIVs(pk, la, lines) && autoCorrectConfig.AutoCorrectIVs)
            RemoveIVLine(lines);

        if (!ValidateEVs(lines) && autoCorrectConfig.AutoCorrectEVs)
            RemoveEVLine(lines);

        string markLine = null;
        if (autoCorrectConfig.AutoCorrectMarks)
        {
            var markVerifier = new MarkVerifier();
            markVerifier.Verify(la);
            if (!la.Valid)
                markLine = await CorrectMarks(pk, la.EncounterOriginal, lines);
        }

        string correctedHeldItem = autoCorrectConfig.AutoCorrectHeldItem ? ValidateHeldItem(lines, pk, itemlist, heldItem) : heldItem;

        string[] correctedLines = lines.Select((line, i) => CorrectLine(line, i, speciesName, correctedSpeciesName, correctedFormName, gender, correctedHeldItem, correctedAbilityName, correctedNatureName, correctedBallName, levelValue, la, nickname)).ToArray();

        // Find the index of the first move line
        int moveSetIndex = Array.FindIndex(correctedLines, line => line.StartsWith("- "));

        // Insert the mark line above the move set if it exists, otherwise add it at the end
        if (!string.IsNullOrEmpty(markLine))
        {
            if (moveSetIndex != -1)
            {
                var updatedLines = new List<string>(correctedLines);
                updatedLines.Insert(moveSetIndex, markLine);
                correctedLines = updatedLines.ToArray();
            }
            else
            {
                // Replace the invalid mark line with the corrected line
                int invalidMarkIndex = Array.FindIndex(correctedLines, line => line.StartsWith(".RibbonMark"));
                if (invalidMarkIndex != -1)
                    correctedLines[invalidMarkIndex] = markLine;
                else
                    correctedLines = correctedLines.Append(markLine).ToArray();
            }
        }

        if (autoCorrectConfig.AutoCorrectNickname)
        {
            var nicknameVerifier = new NicknameVerifier();
            nicknameVerifier.Verify(la);
            if (!la.Valid)
            {
                string fixedNickname = autoCorrectConfig.FixedNickname;
                correctedLines[0] = CorrectLine(correctedLines[0], 0, speciesName, correctedSpeciesName, correctedFormName, gender, correctedHeldItem, correctedAbilityName, correctedNatureName, correctedBallName, levelValue, la, fixedNickname);
            }
        }

        string finalShowdownSet = string.Join(Environment.NewLine, correctedLines);

        await Task.Run(() => LogUtil.LogInfo($"Final Showdown Set:\n{finalShowdownSet}", nameof(AutoCorrectShowdown<T>)));

        return finalShowdownSet;
    }

    private static (string speciesName, string formName, string gender, string heldItem, string nickname) ParseSpeciesLine(string speciesLine)
    {
        string formName = string.Empty;
        string gender = string.Empty;
        string heldItem = string.Empty;
        string nickname = string.Empty;

        int heldItemIndex = speciesLine.IndexOf(" @ ");
        if (heldItemIndex != -1)
        {
            heldItem = speciesLine[(heldItemIndex + 3)..].Trim();
            speciesLine = speciesLine[..heldItemIndex].Trim();
        }

        string speciesName = speciesLine;

        Match match = Regex.Match(speciesLine, @"^(.*?)\s*\((.*?)\)(\s*\(([MF])\))?$");
        if (match.Success)
        {
            if (match.Groups[1].Success)
                nickname = match.Groups[1].Value.Trim();

            speciesName = match.Groups[2].Value.Trim();

            if (match.Groups[4].Success)
                gender = match.Groups[4].Value.Trim();
        }

        string[] speciesParts = speciesName.Split(new[] { '-' }, 2);
        speciesName = speciesParts[0].Trim();

        if (speciesParts.Length > 1)
        {
            formName = speciesParts[1].Trim();
        }

        return (speciesName, formName, gender, heldItem, nickname);
    }

    private static (string abilityName, string natureName, string ballName, string levelValue) ParseLines(string[] lines)
    {
        string abilityName = string.Empty;
        string natureName = string.Empty;
        string ballName = string.Empty;
        string levelValue = string.Empty;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("Ability:"))
                abilityName = trimmedLine["Ability:".Length..].Trim();
            else if (trimmedLine.EndsWith(" Nature"))
                natureName = trimmedLine[..^" Nature".Length].Trim();
            else if (trimmedLine.StartsWith("Ball:"))
                ballName = trimmedLine["Ball:".Length..].Trim();
            else if (trimmedLine.StartsWith("Level:"))
                levelValue = trimmedLine["Level:".Length..].Trim();
        }

        return (abilityName, natureName, ballName, levelValue);
    }

    private static string CorrectLine(string line, int index, string speciesName, string correctedSpeciesName, string correctedFormName, string gender, string correctedHeldItem, string correctedAbilityName, string correctedNatureName, string correctedBallName, string levelValue, LegalityAnalysis la, string nickname)
    {
        if (index == 0) // Species line
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(nickname))
            {
                sb.Append(nickname);
                sb.Append(" (");
                sb.Append(correctedSpeciesName);
                if (!string.IsNullOrEmpty(correctedFormName))
                {
                    sb.Append("-");
                    sb.Append(correctedFormName);
                }
                sb.Append(")");
            }
            else
            {
                sb.Append(correctedSpeciesName);
                if (!string.IsNullOrEmpty(correctedFormName))
                {
                    sb.Append("-");
                    sb.Append(correctedFormName);
                }
            }

            if (!string.IsNullOrEmpty(gender))
            {
                sb.Append(" (");
                sb.Append(gender);
                sb.Append(")");
            }

            if (!string.IsNullOrEmpty(correctedHeldItem))
            {
                sb.Append(" @ ");
                sb.Append(correctedHeldItem);
            }

            return sb.ToString();
        }
        else if (line.StartsWith("Ability:"))
            return $"Ability: {correctedAbilityName}";
        else if (line.EndsWith(" Nature"))
            return $"{correctedNatureName} Nature";
        else if (line.StartsWith("Ball:"))
            return $"Ball: {correctedBallName}";
        else if (line.StartsWith("Level:"))
            return !la.Valid ? "Level: 100" : $"Level: {levelValue}";

        return line;
    }

    private static Task<string>? GetClosestSpecies(string userSpecies)
    {
        var gameStrings = GetGameStrings();
        var pkms = gameStrings.specieslist.Select(s => new T { Species = (ushort)Array.IndexOf(gameStrings.specieslist, s) });
        var sortedSpecies = pkms.OrderBySpeciesName(gameStrings.specieslist).Select(p => gameStrings.specieslist[p.Species]);

        var speciesName = userSpecies.Split('-')[0].Trim();

        var fuzzySpecies = sortedSpecies
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => (Species: s, Distance: Fuzz.Ratio(speciesName, s)))
            .OrderByDescending(s => s.Distance)
            .FirstOrDefault();

        return Task.FromResult<string>(fuzzySpecies.Distance >= 80 ? fuzzySpecies.Species : null);
    }

    private static Task<string>? GetClosestFormName(string userFormName, string[] validFormNames)
    {
        var fuzzyFormName = validFormNames
            .Where(f => !string.IsNullOrEmpty(f))
            .Select(f => (FormName: f, Distance: Fuzz.Ratio(userFormName, f)))
            .OrderByDescending(f => f.Distance)
            .ThenBy(f => f.FormName.Length)
            .FirstOrDefault();

        return Task.FromResult<string>(fuzzyFormName.Distance >= 80 ? fuzzyFormName.FormName : null);
    }

    private static string ValidateHeldItem(string[] lines, PKM pk, string[] itemlist, string heldItem)
    {
        // LogUtil.LogInfo($"Validating held item: {heldItem}", nameof(AutoCorrectShowdown<T>));

        if (!string.IsNullOrEmpty(heldItem))
        {
            string correctedHeldItem = GetClosestItem(heldItem, itemlist);
            // LogUtil.LogInfo($"Corrected held item: {correctedHeldItem}", nameof(AutoCorrectShowdown<T>));

            if (correctedHeldItem != null)
            {
                int itemIndex = Array.IndexOf(itemlist, correctedHeldItem);
                // LogUtil.LogInfo($"Item index: {itemIndex}", nameof(AutoCorrectShowdown<T>));

                if (ItemRestrictions.IsHeldItemAllowed(itemIndex, pk.Context))
                {
                    // LogUtil.LogInfo("Held item is allowed", nameof(AutoCorrectShowdown<T>));

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (line.Contains(" @ "))
                        {
                            lines[i] = line.Replace(heldItem, correctedHeldItem);
                            // LogUtil.LogInfo($"Updated line: {lines[i]}", nameof(AutoCorrectShowdown<T>));
                            break;
                        }
                    }

                    return correctedHeldItem;
                }
                else
                {
                    // LogUtil.LogInfo("Held item is not allowed", nameof(AutoCorrectShowdown<T>));

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (line.Contains(" @ "))
                        {
                            lines[i] = line.Split(new[] { " @ " }, StringSplitOptions.None)[0];
                            // LogUtil.LogInfo($"Updated line: {lines[i]}", nameof(AutoCorrectShowdown<T>));
                            break;
                        }
                    }

                    return string.Empty;
                }
            }
        }

        return heldItem;
    }

    private static string? GetClosestItem(string userItem, string[] itemlist)
    {
        // LogUtil.LogInfo($"Getting closest item for: {userItem}", nameof(AutoCorrectShowdown<T>));

        var fuzzyItem = itemlist
            .Where(item => !string.IsNullOrEmpty(item))
            .Select(item => (Item: item, Distance: Fuzz.Ratio(userItem.ToLower(), item.ToLower())))
            .OrderByDescending(item => item.Distance)
            .FirstOrDefault();

        // LogUtil.LogInfo($"Closest item: {fuzzyItem.Item}, Distance: {fuzzyItem.Distance}", nameof(AutoCorrectShowdown<T>));

        return fuzzyItem.Distance >= 80 ? fuzzyItem.Item : null;
    }

    private static void ValidateMoves(string[] lines, PKM pk, LegalityAnalysis la, GameStrings gameStrings, string speciesName, string formName)
    {
        var moveLines = lines.Where(line => line.StartsWith($"- ")).ToArray();
        var correctedMoveLines = new List<string>(); // Create a list to store corrected move lines

        var validMoves = GetValidMoves(pk, gameStrings, speciesName, formName);

        foreach (var moveLine in moveLines)
        {
            var moveName = moveLine[2..].Trim();
            var correctedMoveName = GetClosestMove(moveName, validMoves);

            if (!string.IsNullOrEmpty(correctedMoveName) && !correctedMoveLines.Contains($"- {correctedMoveName}"))
            {
                correctedMoveLines.Add($"- {correctedMoveName}");
            }
        }

        // Replace the original move lines with the corrected move lines
        for (int i = 0; i < moveLines.Length; i++)
        {
            var moveLine = moveLines[i];
            if (i < correctedMoveLines.Count)
            {
                lines[Array.IndexOf(lines, moveLine)] = correctedMoveLines[i];
                LogUtil.LogInfo(correctedMoveLines[i], nameof(AutoCorrectShowdown<T>));
            }
            else
            {
                lines = lines.Where(line => line != moveLine).ToArray();
            }
        }
    }

    private static string[] GetValidMoves(PKM pk, GameStrings gameStrings, string speciesName, string formName)
    {
        var speciesIndex = Array.IndexOf(gameStrings.specieslist, speciesName);
        var form = pk.Form;

        var learnSource = GetLearnSource(pk);
        var validMoves = new List<string>();

        if (learnSource is LearnSource9SV learnSource9SV)
        {
            if (learnSource9SV.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
            {
                var evo = new EvoCriteria
                {
                    Species = (ushort)speciesIndex,
                    Form = form,
                    LevelMax = 100,
                };

                // Level-up moves
                var learnset = learnSource9SV.GetLearnset((ushort)speciesIndex, form);
                validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // Egg moves
                var eggMoves = learnSource9SV.GetEggMoves((ushort)speciesIndex, form).ToArray();
                validMoves.AddRange(eggMoves
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // Reminder moves
                var reminderMoves = learnSource9SV.GetReminderMoves((ushort)speciesIndex, form).ToArray();
                validMoves.AddRange(reminderMoves
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // TM moves
                var tmMoves = personalInfo.RecordPermitIndexes.ToArray();
                validMoves.AddRange(tmMoves
                    .Where(m => personalInfo.GetIsLearnTM(Array.IndexOf(tmMoves, m)))
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));
            }
        }
        else if (learnSource is LearnSource8BDSP learnSource8BDSP)
        {
            if (learnSource8BDSP.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
            {
                var evo = new EvoCriteria
                {
                    Species = (ushort)speciesIndex,
                    Form = form,
                    LevelMax = 100,
                };

                // Level-up moves
                var learnset = learnSource8BDSP.GetLearnset((ushort)speciesIndex, form);
                validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // Egg moves
                var eggMoves = learnSource8BDSP.GetEggMoves((ushort)speciesIndex, form).ToArray();
                validMoves.AddRange(eggMoves
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // TM moves
                var tmMoves = LearnSource8BDSP.TMHM_BDSP.ToArray();
                validMoves.AddRange(tmMoves
                    .Where(m => personalInfo.GetIsLearnTM(Array.IndexOf(tmMoves, m)))
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));
            }
        }
        else if (learnSource is LearnSource8LA learnSource8LA)
        {
            if (learnSource8LA.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
            {
                var evo = new EvoCriteria
                {
                    Species = (ushort)speciesIndex,
                    Form = form,
                    LevelMax = 100,
                };

                // Level-up moves
                var learnset = learnSource8LA.GetLearnset((ushort)speciesIndex, form);
                validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // Move shop (TM) moves
                var tmMoves = personalInfo.RecordPermitIndexes.ToArray();
                validMoves.AddRange(tmMoves
                    .Where(m => personalInfo.GetIsLearnMoveShop(m))
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));
            }
        }
        else if (learnSource is LearnSource8SWSH learnSource8SWSH)
        {
            if (learnSource8SWSH.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
            {
                var evo = new EvoCriteria
                {
                    Species = (ushort)speciesIndex,
                    Form = form,
                    LevelMax = 100,
                };

                // Level-up moves
                var learnset = learnSource8SWSH.GetLearnset((ushort)speciesIndex, form);
                validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // Egg moves
                var eggMoves = learnSource8SWSH.GetEggMoves((ushort)speciesIndex, form).ToArray();
                validMoves.AddRange(eggMoves
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // TR moves
                var trMoves = personalInfo.RecordPermitIndexes.ToArray();
                validMoves.AddRange(trMoves
                    .Where(m => personalInfo.GetIsLearnTR(Array.IndexOf(trMoves, m)))
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));
            }
        }
        else if (learnSource is LearnSource7GG learnSource7GG)
        {
            if (learnSource7GG.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
            {
                var evo = new EvoCriteria
                {
                    Species = (ushort)speciesIndex,
                    Form = form,
                    LevelMax = 100,
                };

                // Level-up moves (including Move Reminder)
                var learnset = learnSource7GG.GetLearnset((ushort)speciesIndex, form);
                validMoves.AddRange(learnset.GetMoveRange(100).ToArray() // 100 is the bonus for Move Reminder in LGPE
                    .Select(m => gameStrings.movelist[m])
                    .Where(m => !string.IsNullOrEmpty(m)));

                // TM moves and special tutor moves
                for (int move = 0; move < gameStrings.movelist.Length; move++)
                {
                    var learnInfo = learnSource7GG.GetCanLearn(pk, personalInfo, evo, (ushort)move);
                    if (learnInfo.Method is TMHM or Tutor)
                    {
                        var moveName = gameStrings.movelist[move];
                        if (!string.IsNullOrEmpty(moveName))
                            validMoves.Add(moveName);
                    }
                }
            }
        }
        return validMoves.Distinct().ToArray();
    }

    private static string GetClosestMove(string userMove, string[] validMoves)
    {
        // LogUtil.LogInfo($"User move: {userMove}", nameof(AutoCorrectShowdown<T>)); // Debug Stuff
        // LogUtil.LogInfo($"Valid moves: {string.Join(", ", validMoves)}", nameof(AutoCorrectShowdown<T>)); // Debug Stuff

        var fuzzyMove = validMoves
            .Select(m => (Move: m, Distance: Fuzz.Ratio(userMove.ToLower(), m.ToLower())))
            .OrderByDescending(m => m.Distance)
            .FirstOrDefault();

        // LogUtil.LogInfo($"Closest move: {fuzzyMove.Move}, Distance: {fuzzyMove.Distance}", nameof(AutoCorrectShowdown<T>)); // Debug Stuff

        return fuzzyMove.Move;
    }

    private static ILearnSource GetLearnSource(PKM pk)
    {
        if (pk is PK9)
            return LearnSource9SV.Instance;
        if (pk is PB8)
            return LearnSource8BDSP.Instance;
        if (pk is PA8)
            return LearnSource8LA.Instance;
        if (pk is PK8)
            return LearnSource8SWSH.Instance;
        if (pk is PB7)
            return LearnSource7GG.Instance;
        throw new ArgumentException("Unsupported PKM type.", nameof(pk));
    }

    private static Task<string>? GetClosestAbility(string userAbility, ushort speciesIndex, GameStrings gameStrings, IPersonalAbility12 personalInfo)
    {
        var abilities = Enumerable.Range(0, personalInfo.AbilityCount)
            .Select(i => gameStrings.abilitylist[personalInfo.GetAbilityAtIndex(i)])
            .Where(a => !string.IsNullOrEmpty(a));

        var fuzzyAbility = abilities
            .Select(a => (Ability: a, Distance: Fuzz.Ratio(userAbility, a)))
            .OrderByDescending(a => a.Distance)
            .FirstOrDefault();

        return Task.FromResult<string>(fuzzyAbility.Distance >= 80 ? fuzzyAbility.Ability : null);
    }

    private static Task<string>? GetClosestNature(string userNature, GameStrings gameStrings)
    {
        var fuzzyNature = gameStrings.natures
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => (Nature: n, Distance: Fuzz.Ratio(userNature, n)))
            .OrderByDescending(n => n.Distance)
            .FirstOrDefault();

        return Task.FromResult<string>(fuzzyNature.Distance >= 80 ? fuzzyNature.Nature : null);
    }

    private static Task<string>? GetLegalBall(ushort speciesIndex, string formName, string ballName, GameStrings gameStrings, PKM pk)
    {
        var closestBall = GetClosestBall(ballName, gameStrings);

        if (closestBall != null)
        {
            pk.Ball = (byte)Array.IndexOf(gameStrings.itemlist, closestBall);
            if (new LegalityAnalysis(pk).Valid)
                return Task.FromResult(closestBall);
        }

        var legalBall = BallApplicator.ApplyBallLegalByColor(pk);
        return Task.FromResult(gameStrings.itemlist[legalBall]);
    }

    private static string GetClosestBall(string userBall, GameStrings gameStrings)
    {
        var ballList = gameStrings.balllist.Where(b => !string.IsNullOrWhiteSpace(b)).ToArray();

        var fuzzyBall = ballList
            .Select(b => (BallName: b, Distance: Fuzz.PartialRatio(userBall, b)))
            .OrderByDescending(b => b.Distance)
            .FirstOrDefault();

        return fuzzyBall != default ? fuzzyBall.BallName : null;
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

    private static IPersonalFormInfo GetPersonalFormInfo(ushort speciesIndex)
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

        throw new ArgumentException("Type does not have a recognized personal form table.", typeof(T).Name);
    }

    private static EntityContext GetGeneration()
    {
        if (typeof(T) == typeof(PK8))
            return EntityContext.Gen8;
        if (typeof(T) == typeof(PB8))
            return EntityContext.Gen8b;
        if (typeof(T) == typeof(PA8))
            return EntityContext.Gen8a;
        if (typeof(T) == typeof(PK9))
            return EntityContext.Gen9;
        if (typeof(T) == typeof(PB7))
            return EntityContext.Gen7b;

        throw new ArgumentException("Type does not have a recognized generation.", typeof(T).Name);
    }

    private static int GetLanguageIndex(GameVersion version)
    {
        var language = GameLanguage.DefaultLanguage;
        return GameLanguage.GetLanguageIndex(language);
    }

    private static bool ValidateIVs(PKM pk, LegalityAnalysis la, string[] lines)
    {
        var ivVerifier = new IndividualValueVerifier();
        ivVerifier.Verify(la);

        return la.Valid;
    }

    private static bool ValidateEVs(string[] lines)
    {
        int totalEVs = 0;
        string originalEVs = string.Empty;
        string modifiedEVs = string.Empty;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("EVs:"))
            {
                originalEVs = line;
                string[] evParts = line.Replace("EVs:", "").Split('/');
                int[] evValues = new int[6];
                string[] statNames = { "HP", "Atk", "Def", "SpA", "SpD", "Spe" };

                foreach (string evPart in evParts)
                {
                    string[] evValue = evPart.Trim().Split(' ');
                    if (evValue.Length == 2 && int.TryParse(evValue[0], out int value))
                    {
                        string statName = evValue[1];
                        int statIndex = Array.IndexOf(statNames, statName);
                        if (statIndex >= 0)
                        {
                            evValues[statIndex] = value;
                            totalEVs += value;
                        }
                    }
                }

                if (totalEVs <= EffortValues.Max510)
                {
                    return true;
                }
                else
                {
                    // EVs exceed the maximum, correct them proportionally
                    double scaleFactor = (double)EffortValues.Max510 / totalEVs;
                    for (int j = 0; j < evValues.Length; j++)
                    {
                        evValues[j] = (int)Math.Round(evValues[j] * scaleFactor);
                    }

                    modifiedEVs = "EVs: " + string.Join(" / ", evValues.Select((ev, index) => $"{ev} {statNames[index]}"));
                    lines[i] = modifiedEVs;
                    return true;
                }
            }
        }

        return true;
    }

    private static Task<string>? ValidateGender(PKM pk, string gender, string speciesName)
    {
        if (!string.IsNullOrEmpty(gender))
        {
            var gv = new GenderVerifier();
            var la = new LegalityAnalysis(pk);
            gv.Verify(la);

            if (!la.Valid)
            {
                gender = string.Empty;
            }
        }

        return Task.FromResult(gender);
    }

    private static void RemoveIVLine(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("IVs:"))
            {
                lines = lines.Where((line, index) => index != i).ToArray();
                break;
            }
        }
    }

    private static void RemoveEVLine(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("EVs:"))
            {
                lines = lines.Where((line, index) => index != i).ToArray();
                break;
            }
        }
    }

    private static Task<string>? CorrectMarks(PKM pk, IEncounterTemplate encounter, string[] lines)
    {
        if (pk is not IRibbonIndex m)
        {
            LogUtil.LogInfo("PKM does not implement IRibbonIndex. Correcting to '.Ribbons=$SuggestAll'.", nameof(AutoCorrectShowdown<T>));
            return Task.FromResult(".Ribbons=$SuggestAll");
        }

        // Find the existing mark line in the input showdown set
        string existingMarkLine = lines.FirstOrDefault(line => line.StartsWith(".RibbonMark"));

        if (!string.IsNullOrEmpty(existingMarkLine))
        {
            // Extract the mark name from the existing mark line
            string markName = existingMarkLine.Split('=')[0].Replace(".RibbonMark", string.Empty);

            // Find the corresponding RibbonIndex based on the mark name
            if (Enum.TryParse($"Mark{markName}", out RibbonIndex markIndex))
            {
                // Check if the mark is valid based on the encounter and the Pokémon
                if (MarkRules.IsEncounterMarkValid(markIndex, pk, encounter))
                {
                    m.SetRibbon((int)markIndex, true);
                    LogUtil.LogInfo($"Found valid mark: {markIndex}. Keeping the existing mark line: {existingMarkLine}", nameof(AutoCorrectShowdown<T>));
                    return Task.FromResult(existingMarkLine);
                }
                else
                {
                    LogUtil.LogInfo($"Mark {markIndex} is not valid for the encounter. Correcting to '.Ribbons=$SuggestAll'.", nameof(AutoCorrectShowdown<T>));
                }
            }
            else
            {
                LogUtil.LogInfo($"Invalid mark name: {markName}. Correcting to '.Ribbons=$SuggestAll'.", nameof(AutoCorrectShowdown<T>));
            }
        }

        // Apply valid marks based on the encounter if no valid mark is found
        if (MarkRules.IsEncounterMarkAllowed(encounter, pk))
        {
            LogUtil.LogInfo("Encounter allows marks. Searching for valid marks.", nameof(AutoCorrectShowdown<T>));
            for (var mark = MarkLunchtime; mark <= MarkSlump; mark++)
            {
                if (MarkRules.IsEncounterMarkValid(mark, pk, encounter))
                {
                    m.SetRibbon((int)mark, true);
                    LogUtil.LogInfo($"Found valid mark: {mark}. Setting the mark line to '.RibbonMark{GetRibbonNameSafe(mark)}=True'.", nameof(AutoCorrectShowdown<T>));
                    return Task.FromResult($".RibbonMark{GetRibbonNameSafe(mark)}=True");
                }
            }
        }
        else
        {
            LogUtil.LogInfo("Encounter does not allow marks. Correcting to '.Ribbons=$SuggestAll'.", nameof(AutoCorrectShowdown<T>));
        }

        // If no valid mark is found, correct the line to ".Ribbons=$SuggestAll"
        LogUtil.LogInfo("No valid marks found. Correcting to '.Ribbons=$SuggestAll'.", nameof(AutoCorrectShowdown<T>));
        return Task.FromResult(".Ribbons=$SuggestAll");
    }

    private static string GetRibbonNameSafe(RibbonIndex index)
    {
        if (index >= MAX_COUNT)
            return index.ToString();
        var expect = $"Ribbon{index}";
        return RibbonStrings.GetName(expect);
    }
}
