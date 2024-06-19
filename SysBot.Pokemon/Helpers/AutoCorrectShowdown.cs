using FuzzySharp;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PKHeX.Core.LearnMethod;
using static PKHeX.Core.RibbonIndex;

namespace SysBot.Pokemon;

public static class AutoCorrectShowdown<T> where T : PKM, new()
{
    private static readonly char[] separator = ['\r', '\n'];

    public static async Task<(string CorrectedContent, List<string> CorrectionMessages)> PerformAutoCorrect(string content, PKM originalPk, LegalityAnalysis originalLa)
    {
        var autoCorrectConfig = new TradeSettings.AutoCorrectShowdownCategory();
        if (!autoCorrectConfig.EnableAutoCorrect)
            return (content, new List<string>());

        string[] lines = content.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        var gameStrings = GetGameStrings();
        var generation = GetGeneration();
        var itemlist = gameStrings.itemlist;

        (string speciesName, string formName, string gender, string heldItem, string nickname) = ParseSpeciesLine(lines[0]);

        string correctedSpeciesName = autoCorrectConfig.AutoCorrectSpeciesAndForm ? await GetClosestSpecies(speciesName).ConfigureAwait(false) ?? speciesName : speciesName;
        ushort speciesIndex = (ushort)Array.IndexOf(gameStrings.specieslist, correctedSpeciesName);
        string[] formNames = FormConverter.GetFormList(speciesIndex, gameStrings.types, gameStrings.forms, new List<string>(), generation);

        string correctedFormName = formName;
        if (!string.IsNullOrEmpty(formName))
        {
            correctedFormName = await GetClosestFormName(formName, formNames).ConfigureAwait(false) ?? formName;
        }

        // Combine corrected species and form names
        string finalCorrectedName = string.IsNullOrEmpty(correctedFormName) ? correctedSpeciesName : $"{correctedSpeciesName}-{correctedFormName}";

        // Continue with your logic using finalCorrectedName
        PKM pk = originalPk.Clone();
        LegalityAnalysis la = originalLa;

        var correctionMessages = new List<string>();

        bool speciesOrFormCorrected = finalCorrectedName != $"{speciesName}-{formName}".Trim('-');
        if (speciesOrFormCorrected)
        {
            pk.Species = speciesIndex;
            var personalFormInfo = await Task.Run(() => GetPersonalFormInfo(speciesIndex)).ConfigureAwait(false);
            pk.Form = correctedFormName != null ? (byte)personalFormInfo.FormIndex(speciesIndex, (byte)Array.IndexOf(formNames, correctedFormName)) : (byte)0;
            la = new LegalityAnalysis(pk);

            correctionMessages.Add($"Species or form was incorrect. Adjusted to **{finalCorrectedName}**.");
        }

        (string abilityName, string natureName, string ballName, string levelValue) = ParseLines(lines);

        var personalAbilityInfoTask = Task.Run(() => GetPersonalInfo(speciesIndex));
        var (closestAbility, abilityCorrectd) = await GetClosestAbility(abilityName, speciesIndex, gameStrings, await personalAbilityInfoTask);
        var (closestNature, natureCorrectd) = await GetClosestNature(natureName, gameStrings);

        string correctedAbilityName = autoCorrectConfig.AutoCorrectAbility ? closestAbility ?? abilityName : abilityName;
        string correctedNatureName = autoCorrectConfig.AutoCorrectNature ? closestNature ?? natureName : natureName;

        if (abilityCorrectd)
        {
            correctionMessages.Add($"Ability was incorrect. Adjusted from **{abilityName}** to **{correctedAbilityName}**.");
        }

        if (natureCorrectd)
        {
            correctionMessages.Add($"Nature was incorrect. Adjusted from **{natureName}** to **{correctedNatureName}**.");
        }

        string formNameForBallVerification = correctedSpeciesName == speciesName ? formName : correctedFormName;
        string correctedBallName = string.Empty;

        if (!string.IsNullOrEmpty(ballName))
        {
            var legalBallTask = GetLegalBall(speciesIndex, formNameForBallVerification, ballName, gameStrings, pk);
            correctedBallName = autoCorrectConfig.AutoCorrectBall ? await legalBallTask : ballName;

            if (!string.IsNullOrEmpty(correctedBallName) && correctedBallName != ballName)
            {
                correctionMessages.Add($"{correctedSpeciesName} can't be in a {ballName}. Adjusted to **{correctedBallName}**.");
            }
        }

        if (!string.IsNullOrEmpty(correctedAbilityName) && correctedAbilityName != abilityName)
        {
            correctionMessages.Add($"{speciesName} can't have the ability {abilityName}. Adjusted to **{correctedAbilityName}**.");
        }

        if (!string.IsNullOrEmpty(correctedNatureName) && correctedNatureName != natureName)
        {
            correctionMessages.Add($"Nature was incorrect. Adjusted to **{correctedNatureName}** Nature.");
        }

        var levelVerifier = new LevelVerifier();
        if (autoCorrectConfig.AutoCorrectLevel && !string.IsNullOrWhiteSpace(levelValue))
        {
            levelVerifier.Verify(la);
            if (!la.Valid)
            {
                correctionMessages.Add($"Level was incorrect. Adjusted to **Level 100**.");
            }
        }

        if (autoCorrectConfig.AutoCorrectMovesLearnset)
            ValidateMoves(lines, pk, la, gameStrings, correctedSpeciesName, correctedFormName ?? formName, correctionMessages);

        if (!ValidateIVs(pk, la, lines) && autoCorrectConfig.AutoCorrectIVs)
            RemoveIVLine(lines);

        if (!ValidateEVs(lines) && autoCorrectConfig.AutoCorrectEVs)
            RemoveEVLine(lines);

        VerifyShiny(pk, la, lines, correctionMessages);

        string? markLine = null;
        if (autoCorrectConfig.AutoCorrectMarks)
        {
            var markVerifier = new MarkVerifier();
            markVerifier.Verify(la);
            if (!la.Valid)
                markLine = await CorrectMarks(pk, la.EncounterOriginal, lines);
        }

        string correctedHeldItem = autoCorrectConfig.AutoCorrectHeldItem ? ValidateHeldItem(lines, pk, itemlist, heldItem) : heldItem;

        string correctedGender = gender;
        if (autoCorrectConfig.AutoCorrectGender)
        {
            correctedGender = await ValidateGender(pk, gender, speciesName);
            if (!string.IsNullOrEmpty(correctedGender) && correctedGender != gender)
            {
                correctionMessages.Add($"{speciesName} can't be {gender}. Adjusted to **{correctedGender}**.");
            }
            else
            {
                correctedGender = gender;
            }
        }

        string[] correctedLines = lines.Select((line, i) => CorrectLine(line, i, speciesName, correctedSpeciesName, correctedFormName ?? formName, formName, correctedGender, correctedHeldItem, correctedAbilityName, correctedNatureName, correctedBallName, levelValue, la, nickname)).ToArray();

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

        string finalShowdownSet = string.Join(Environment.NewLine, correctedLines);

       // await Task.Run(() => LogUtil.LogInfo($"Final Showdown Set:\n{finalShowdownSet}", nameof(AutoCorrectShowdown<T>)));

        return (finalShowdownSet, correctionMessages);
    }

    private static (string speciesName, string formName, string gender, string heldItem, string nickname) ParseSpeciesLine(string speciesLine)
    {
        string formName = string.Empty;
        string gender = string.Empty;
        string heldItem = string.Empty;
        string nickname = string.Empty;
        string speciesName = string.Empty;

        int heldItemIndex = speciesLine.IndexOf(" @ ");
        if (heldItemIndex != -1)
        {
            heldItem = speciesLine[(heldItemIndex + 3)..].Trim();
            speciesLine = speciesLine[..heldItemIndex].Trim();
            //LogUtil.LogInfo($"Parsed held item: {heldItem}", nameof(ParseSpeciesLine));
        }

        int firstParenIndex = speciesLine.IndexOf('(');
        int lastParenIndex = speciesLine.LastIndexOf(')');

        if (firstParenIndex != -1 && lastParenIndex != -1 && firstParenIndex < lastParenIndex)
        {
            string textInParentheses = speciesLine[(firstParenIndex + 1)..lastParenIndex].Trim();

            if (textInParentheses == "M" || textInParentheses == "F")
            {
                gender = textInParentheses;
               // LogUtil.LogInfo($"Parsed gender: {gender}", nameof(ParseSpeciesLine));
                speciesName = speciesLine[..firstParenIndex].Trim();
                //LogUtil.LogInfo($"Parsed species name: {speciesName}", nameof(ParseSpeciesLine));
            }
            else
            {
                int secondParenIndex = textInParentheses.IndexOf('(');
                if (secondParenIndex != -1)
                {
                    string genderInParentheses = textInParentheses[(secondParenIndex + 1)..].Trim();
                    if (genderInParentheses == "M" || genderInParentheses == "F")
                    {
                        gender = genderInParentheses;
                        //LogUtil.LogInfo($"Parsed gender: {gender}", nameof(ParseSpeciesLine));
                        textInParentheses = textInParentheses[..secondParenIndex].Trim();
                    }
                }

                speciesName = textInParentheses;
                //LogUtil.LogInfo($"Parsed species name: {speciesName}", nameof(ParseSpeciesLine));

                string remainingText = speciesLine[..firstParenIndex].Trim();
                if (!string.IsNullOrEmpty(remainingText))
                {
                    nickname = remainingText;
                    //LogUtil.LogInfo($"Parsed nickname: {nickname}", nameof(ParseSpeciesLine));
                }
            }
        }
        else
        {
            speciesName = speciesLine.Trim();
            //LogUtil.LogInfo($"Parsed species name: {speciesName}", nameof(ParseSpeciesLine));
        }

        speciesName = speciesName.Replace(")", string.Empty);

        int formSeparatorIndex = speciesName.IndexOf('-');
        if (formSeparatorIndex != -1)
        {
            formName = speciesName[(formSeparatorIndex + 1)..].Trim();
            speciesName = speciesName[..formSeparatorIndex].Trim();
            //LogUtil.LogInfo($"Parsed form name: {formName}", nameof(ParseSpeciesLine));
        }

        //LogUtil.LogInfo($"Final parsed values - Species: {speciesName}, Form: {formName}, Gender: {gender}, Held Item: {heldItem}, Nickname: {nickname}", nameof(ParseSpeciesLine));
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

    private static string CorrectLine(string line, int index, string speciesName, string correctedSpeciesName, string correctedFormName, string formName, string correctedGender, string correctedHeldItem, string correctedAbilityName, string correctedNatureName, string correctedBallName, string levelValue, LegalityAnalysis la, string nickname)
    {
        if (index == 0) // Species line
        {
            StringBuilder sb = new StringBuilder();
            string finalCorrectedName = string.IsNullOrEmpty(correctedFormName) ? correctedSpeciesName : $"{correctedSpeciesName}-{correctedFormName}";
            if (!string.IsNullOrEmpty(nickname))
            {
                sb.Append(nickname);
                sb.Append(" (");
                sb.Append(finalCorrectedName);
                sb.Append(')');
            }
            else
            {
                sb.Append(finalCorrectedName);
            }
            if (!string.IsNullOrEmpty(correctedGender))
            {
                sb.Append(" (");
                sb.Append(correctedGender);
                sb.Append(')');
            }
            if (!string.IsNullOrEmpty(correctedHeldItem))
            {
                sb.Append(" @ ");
                sb.Append(correctedHeldItem);
            }
            return sb.ToString();
        }
        else if (line.StartsWith("Ability:"))
        {
            return $"Ability: {correctedAbilityName}";
        }
        else if (line.EndsWith(" Nature"))
        {
            return $"{correctedNatureName} Nature";
        }
        else if (line.StartsWith("Ball:"))
        {
            return $"Ball: {correctedBallName}";
        }
        else if (line.StartsWith("Level:"))
        {
            return !la.Valid ? "Level: 100" : $"Level: {levelValue}";
        }
        else if (line.StartsWith("Shiny:", StringComparison.OrdinalIgnoreCase))
        {
            return la.EncounterMatch.Shiny.IsValid(la.Entity) ? "Shiny: Yes" : "Shiny: No";
        }
        return line;
    }

    private static async Task<string?> GetClosestSpecies(string userSpecies, string[]? formNames = null)
    {
        var gameStrings = GetGameStrings();
        var sortedSpecies = gameStrings.specieslist.OrderBy(name => name);

        var speciesName = userSpecies.Split('-')[0].Trim();
        var formNamePart = userSpecies.Contains('-') ? string.Join("-", userSpecies.Split('-').Skip(1)).Trim() : string.Empty;

        // LogUtil.LogInfo($"Comparing species: {speciesName}", nameof(AutoCorrectShowdown<T>));

        var fuzzySpecies = sortedSpecies
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => (Species: s, Distance: Fuzz.Ratio(speciesName, s)))
            .OrderByDescending(s => s.Distance)
            .FirstOrDefault();

        // LogUtil.LogInfo($"Closest species found: {fuzzySpecies.Species} with distance {fuzzySpecies.Distance}", nameof(AutoCorrectShowdown<T>));

        if (fuzzySpecies.Distance >= 80)
        {
            var correctedSpecies = fuzzySpecies.Species;

            if (!string.IsNullOrEmpty(formNamePart))
            {
                if (formNames != null)
                {
                    var closestFormName = await GetClosestFormName(formNamePart, formNames).ConfigureAwait(false);
                    // LogUtil.LogInfo($"Form names being compared against: {string.Join(", ", formNames)}", nameof(AutoCorrectShowdown<T>));
                    // LogUtil.LogInfo($"Closest form found: {closestFormName}", nameof(AutoCorrectShowdown<T>));

                    if (closestFormName != null)
                    {
                        return $"{correctedSpecies}-{closestFormName}";
                    }
                }
                return $"{correctedSpecies}-{formNamePart}";
            }
            return correctedSpecies;
        }
        return null;
    }

    private static Task<string?> GetClosestFormName(string userFormName, string[] validFormNames)
    {
        // LogUtil.LogInfo($"Comparing form name: {userFormName}", nameof(AutoCorrectShowdown<T>));

        var fuzzyFormName = validFormNames
            .Where(f => !string.IsNullOrEmpty(f))
            .Select(f => (FormName: f, Distance: Fuzz.Ratio(userFormName, f)))
            .OrderByDescending(f => f.Distance)
            .ThenBy(f => f.FormName.Length)
            .FirstOrDefault();

        // LogUtil.LogInfo($"Closest form name found: {fuzzyFormName.FormName} with distance {fuzzyFormName.Distance}", nameof(AutoCorrectShowdown<T>));

        // Lowered threshold to 70 and added fallback logic
        if (fuzzyFormName.Distance >= 70)
        {
            return Task.FromResult<string?>(fuzzyFormName.FormName);
        }

        // Fallback: return the closest match even if it doesn't meet the threshold
        return Task.FromResult<string?>(fuzzyFormName.FormName);
    }

    private static string ValidateHeldItem(string[] lines, PKM pk, string[] itemlist, string heldItem)
    {
        // LogUtil.LogInfo($"Validating held item: {heldItem}", nameof(AutoCorrectShowdown<T>));

        if (!string.IsNullOrEmpty(heldItem))
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string correctedHeldItem = GetClosestItem(heldItem, itemlist);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

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

    private static void VerifyShiny(PKM pk, LegalityAnalysis la, string[] lines, List<string> correctionMessages)
    {
        var enc = la.EncounterMatch;
        if (!enc.Shiny.IsValid(pk))
        {
            string speciesName = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);
            correctionMessages.Add($"{speciesName} cannot be shiny. Setting to **Shiny: No**.");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Shiny: Yes", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = "Shiny: No";
                    break;
                }
            }
        }
    }

    private static void ValidateMoves(string[] lines, PKM pk, LegalityAnalysis la, GameStrings gameStrings, string speciesName, string formName, List<string> correctionMessages)
    {
        var moveLines = lines.Where(line => line.StartsWith("- ")).ToArray();
        var correctedMoveLines = new List<string>(); // Create a list to store corrected move lines
        var validMoves = GetValidMoves(pk, gameStrings, speciesName, formName);
        var usedMoves = new HashSet<string>(); // Create a HashSet to track used moves

        for (int i = 0; i < moveLines.Length && i < 4; i++) // Validate up to four moves
        {
            var moveLine = moveLines[i];
            var moveName = moveLine[2..].Trim();
            var correctedMoveName = GetClosestMove(moveName, validMoves);

            if (!string.IsNullOrEmpty(correctedMoveName))
            {
                if (!usedMoves.Contains(correctedMoveName))
                {
                    correctedMoveLines.Add($"- {correctedMoveName}");
                    usedMoves.Add(correctedMoveName);
                    if (moveName != correctedMoveName)
                    {
                        var speciesNameEN = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);
                        correctionMessages.Add($"{speciesNameEN} cannot learn {moveName}. Replaced with **{correctedMoveName}**.");
                    }
                }
                else
                {
                    var unusedValidMoves = validMoves.Except(usedMoves).ToList();
                    if (unusedValidMoves.Count > 0)
                    {
                        var randomMove = unusedValidMoves[new Random().Next(unusedValidMoves.Count)];
                        correctedMoveLines.Add($"- {randomMove}");
                        usedMoves.Add(randomMove);
                        var speciesNameEN = SpeciesName.GetSpeciesNameGeneration(pk.Species, (int)LanguageID.English, pk.Format);
                        correctionMessages.Add($"{speciesNameEN} cannot learn {moveName}. Replaced with **{randomMove}**.");
                    }
                }
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

    private static Task<(string? Ability, bool Corrected)> GetClosestAbility(string userAbility, ushort speciesIndex, GameStrings gameStrings, IPersonalAbility12 personalInfo)
    {
        var abilities = Enumerable.Range(0, personalInfo.AbilityCount)
            .Select(i => gameStrings.abilitylist[personalInfo.GetAbilityAtIndex(i)])
            .Where(a => !string.IsNullOrEmpty(a));

        var fuzzyAbility = abilities
            .Select(a => (Ability: a, Distance: Fuzz.Ratio(userAbility, a)))
            .OrderByDescending(a => a.Distance)
            .FirstOrDefault();

        var correctedAbility = fuzzyAbility.Distance >= 80 ? fuzzyAbility.Ability : null;
        var corrected = correctedAbility != null && correctedAbility != userAbility;
        return Task.FromResult((correctedAbility, corrected));
    }

    private static Task<(string? Nature, bool Corrected)> GetClosestNature(string userNature, GameStrings gameStrings)
    {
        var fuzzyNature = gameStrings.natures
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => (Nature: n, Distance: Fuzz.Ratio(userNature, n)))
            .OrderByDescending(n => n.Distance)
            .FirstOrDefault();

        var correctedNature = fuzzyNature.Distance >= 80 ? fuzzyNature.Nature : null;
        var corrected = correctedNature != null && correctedNature != userNature;
        return Task.FromResult((correctedNature, corrected));
    }

    private static Task<string>? GetLegalBall(ushort speciesIndex, string formNameForBallVerification, string ballName, GameStrings gameStrings, PKM pk)
    {
        var closestBall = GetClosestBall(ballName, gameStrings);
        if (closestBall != null)
        {
            pk.Ball = (byte)Array.IndexOf(gameStrings.balllist, closestBall);
            if (new LegalityAnalysis(pk).Valid)
                return Task.FromResult(closestBall);
        }
        var legalBall = BallApplicator.ApplyBallLegalByColor(pk);
        return Task.FromResult(gameStrings.balllist[legalBall]);
    }

    private static string? GetClosestBall(string userBall, GameStrings gameStrings)
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
        const string language = GameLanguage.DefaultLanguage;
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
                string[] statNames = ["HP", "Atk", "Def", "SpA", "SpD", "Spe"];

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

    private static Task<string> ValidateGender(PKM pk, string gender, string speciesName)
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

    private static Task<string> CorrectMarks(PKM pk, IEncounterTemplate encounter, string[] lines)
    {
        if (pk is not IRibbonIndex m)
        {
            LogUtil.LogInfo("PKM does not implement IRibbonIndex. Correcting to '.Ribbons=$SuggestAll'.", nameof(AutoCorrectShowdown<T>));
            return Task.FromResult(".Ribbons=$SuggestAll");
        }

        // Find the existing mark line in the input showdown set
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        string existingMarkLine = lines.FirstOrDefault(line => line.StartsWith(".RibbonMark"));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

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
