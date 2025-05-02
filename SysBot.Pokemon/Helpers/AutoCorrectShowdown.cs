using PKHeX.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SysBot.Pokemon.Helpers.ShowdownHelpers;

namespace SysBot.Pokemon;

public static class AutoCorrectShowdown<T> where T : PKM, new()
{
    private static readonly char[] separator = ['\r', '\n'];

    public static async Task<(string CorrectedContent, List<string> CorrectionMessages)> PerformAutoCorrect(string content, PKM originalPk, LegalityAnalysis originalLa)
    {
        return await Task.Run(async () =>
        {
            var autoCorrectConfig = new TradeSettings.AutoCorrectShowdownCategory();
            if (!autoCorrectConfig.EnableAutoCorrect)
                return (content, new List<string>());

            string[] lines = content.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var gameStrings = GameInfoHelpers<T>.GetGameStrings();
            var generation = GameInfoHelpers<T>.GetGeneration();
            var itemlist = gameStrings.itemlist;

            (string speciesName, string formName, string gender, string heldItem, string nickname) = ParseSpeciesLine(lines[0]);
            string originalSpeciesName = speciesName;
            string originalFormName = formName;
            string correctedSpeciesName = autoCorrectConfig.AutoCorrectSpeciesAndForm ? await FirstLine<T>.GetClosestSpecies(speciesName) ?? speciesName : speciesName;
            ushort speciesIndex = (ushort)Array.IndexOf(gameStrings.specieslist, correctedSpeciesName);

            string[] formNames = Array.Empty<string>();
            string correctedFormName = formName;
            if (!string.IsNullOrEmpty(formName))
            {
                formNames = FormConverter.GetFormList(speciesIndex, gameStrings.types, gameStrings.forms, new List<string>(), generation);
                correctedFormName = await FirstLine<T>.GetClosestFormName(formName, formNames) ?? formName;
            }

            // Combine corrected species and form names
            string finalCorrectedName = string.IsNullOrEmpty(correctedFormName) ? correctedSpeciesName : $"{correctedSpeciesName}-{correctedFormName}";

            // Update speciesName to be the same as finalCorrectedName
            speciesName = finalCorrectedName;

            PKM pk = originalPk.Clone();
            LegalityAnalysis la = originalLa;

            var correctionMessages = new List<string>();

            bool speciesOrFormCorrected = finalCorrectedName != $"{originalSpeciesName}-{originalFormName}".Trim('-');
            if (speciesOrFormCorrected)
            {
                pk.Species = speciesIndex;
                var personalFormInfo = GameInfoHelpers<T>.GetPersonalFormInfo(speciesIndex);
                pk.Form = correctedFormName != null ? (byte)personalFormInfo.FormIndex(speciesIndex, (byte)Array.IndexOf(formNames, correctedFormName)) : (byte)0;
                la = new LegalityAnalysis(pk);

                // Add separate messages for species and form corrections
                if (correctedSpeciesName != originalSpeciesName)
                {
                    correctionMessages.Add($"Species was incorrect. Adjusted from **{originalSpeciesName}** to **{correctedSpeciesName}**.");
                }
                if (correctedFormName != originalFormName)
                {
                    correctionMessages.Add($"Form was incorrect. Adjusted from **{originalFormName}** to **{correctedFormName}**.");
                }
                if (correctedSpeciesName == originalSpeciesName && correctedFormName == originalFormName)
                {
                    correctionMessages.Add($"Species or form was incorrect. Adjusted to **{finalCorrectedName}**.");
                }
            }

            (string abilityName, string natureName, string ballName) = ParseLines(lines);

            string correctedAbilityName = string.Empty;
            if (!string.IsNullOrEmpty(abilityName) && autoCorrectConfig.AutoCorrectAbility)
            {
                var personalAbilityInfo = GameInfoHelpers<T>.GetPersonalInfo(speciesIndex);
                var closestAbility = await AbilityHelper<T>.GetClosestAbility(abilityName, speciesIndex, gameStrings, personalAbilityInfo);
                correctedAbilityName = closestAbility.Ability ?? abilityName;

                if (!string.IsNullOrEmpty(correctedAbilityName) && correctedAbilityName != abilityName)
                {
                    correctionMessages.Add($"{speciesName} can't have the ability {abilityName}. Adjusted to **{correctedAbilityName}**.");
                }
            }

            string correctedNatureName = string.Empty;
            if (!string.IsNullOrEmpty(natureName) && autoCorrectConfig.AutoCorrectNature)
            {
                var closestNature = await NatureHelper<T>.GetClosestNature(natureName, gameStrings);
                correctedNatureName = closestNature.Nature ?? natureName;

                if (!string.IsNullOrEmpty(correctedNatureName) && correctedNatureName != natureName)
                {
                    correctionMessages.Add($"Nature was incorrect. Adjusted to **{correctedNatureName}** Nature.");
                }
            }

            string formNameForBallVerification = correctedSpeciesName == speciesName ? formName : correctedFormName;
            string correctedBallName = string.Empty;
            if (!string.IsNullOrEmpty(ballName) && autoCorrectConfig.AutoCorrectBall)
            {
                var legalBall = await BallHelper<T>.GetLegalBall(speciesIndex, formNameForBallVerification, ballName, gameStrings, pk);
                correctedBallName = legalBall;

                if (!string.IsNullOrEmpty(correctedBallName) && correctedBallName != ballName)
                {
                    correctionMessages.Add($"{speciesName} can't be in a {ballName}. Adjusted to **{correctedBallName}**.");
                }
            }

            if (autoCorrectConfig.AutoCorrectMovesLearnset && lines.Any(line => line.StartsWith("- ")))
            {
                await MoveHelper<T>.ValidateMovesAsync(lines, pk, la, gameStrings, speciesName, correctedFormName ?? formName, correctionMessages);
            }

            if (autoCorrectConfig.AutoCorrectIVs && lines.Any(line => line.StartsWith("IVs:")))
            {
                if (!IVEVHelper<T>.ValidateIVs(pk, la, lines))
                    IVEVHelper<T>.RemoveIVLine(lines);
            }

            if (autoCorrectConfig.AutoCorrectEVs && lines.Any(line => line.StartsWith("EVs:")))
            {
                if (!IVEVHelper<T>.ValidateEVs(lines))
                    IVEVHelper<T>.RemoveEVLine(lines);
            }

            string? markLine = null;
            List<string> markCorrectionMessages = new List<string>();
            if (autoCorrectConfig.AutoCorrectMarks && lines.Any(line => line.StartsWith(".RibbonMark")))
            {
                var markVerifier = new MarkVerifier();
                markVerifier.Verify(la);
                if (!la.Valid)
                {
                    (markLine, markCorrectionMessages) = await MarkHelper<T>.CorrectMarks(pk, la.EncounterOriginal, lines);
                }
            }

            correctionMessages.AddRange(markCorrectionMessages);

            (string correctedHeldItem, string heldItemCorrectionMessage) = FirstLine<T>.ValidateHeldItem(lines, pk, itemlist, heldItem);

            if (!string.IsNullOrEmpty(heldItemCorrectionMessage))
            {
                correctionMessages.Add(heldItemCorrectionMessage);
            }

            string correctedGender = gender;
            string genderCorrectionMessage = string.Empty;
            if (autoCorrectConfig.AutoCorrectGender && !string.IsNullOrEmpty(gender))
            {
                (correctedGender, genderCorrectionMessage) = FirstLine<T>.ValidateGender(pk, gender, speciesName);
                if (!string.IsNullOrEmpty(genderCorrectionMessage))
                {
                    correctionMessages.Add(genderCorrectionMessage);
                }
            }

            string[] correctedLines = lines.Select((line, i) => CorrectLine(line, i, speciesName, correctedSpeciesName, correctedFormName ?? formName, formName, correctedGender, gender, correctedHeldItem, heldItem, correctedAbilityName, correctedNatureName, correctedBallName, la, nickname)).ToArray();

            // TODO:  Validate Scale Line and remove if necessary.

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

            string finalShowdownSet = string.Join(Environment.NewLine, correctedLines.Where(line => !string.IsNullOrWhiteSpace(line)));

            return (finalShowdownSet, correctionMessages);
        });
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
        }

        int firstParenIndex = speciesLine.IndexOf('(');
        int lastParenIndex = speciesLine.LastIndexOf(')');

        if (firstParenIndex != -1 && lastParenIndex != -1 && firstParenIndex < lastParenIndex)
        {
            string textInParentheses = speciesLine[(firstParenIndex + 1)..lastParenIndex].Trim();

            if (textInParentheses == "M" || textInParentheses == "F")
            {
                gender = textInParentheses;
                speciesName = speciesLine[..firstParenIndex].Trim();
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
                        textInParentheses = textInParentheses[..secondParenIndex].Trim();
                    }
                }

                speciesName = textInParentheses;

                string remainingText = speciesLine[..firstParenIndex].Trim();
                if (!string.IsNullOrEmpty(remainingText))
                {
                    nickname = remainingText;
                }
            }
        }
        else
        {
            speciesName = speciesLine.Trim();
        }

        speciesName = speciesName.Replace(")", string.Empty);

        int formSeparatorIndex = speciesName.IndexOf('-');
        if (formSeparatorIndex != -1)
        {
            formName = speciesName[(formSeparatorIndex + 1)..].Trim();
            speciesName = speciesName[..formSeparatorIndex].Trim();
        }

        return (speciesName, formName, gender, heldItem, nickname);
    }

    private static (string abilityName, string natureName, string ballName) ParseLines(string[] lines)
    {
        string abilityName = string.Empty;
        string natureName = string.Empty;
        string ballName = string.Empty;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("Ability:"))
                abilityName = trimmedLine["Ability:".Length..].Trim();
            else if (trimmedLine.EndsWith(" Nature"))
                natureName = trimmedLine[..^" Nature".Length].Trim();
            else if (trimmedLine.StartsWith("Ball:"))
                ballName = trimmedLine["Ball:".Length..].Trim();
        }

        return (abilityName, natureName, ballName);
    }

    private static string CorrectLine(string line, int index, string speciesName, string correctedSpeciesName, string correctedFormName, string formName, string correctedGender, string gender, string correctedHeldItem, string heldItem, string correctedAbilityName, string correctedNatureName, string correctedBallName, LegalityAnalysis la, string nickname)
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

            // Add gender if present
            if (!string.IsNullOrEmpty(correctedGender))
            {
                sb.Append(" (");
                sb.Append(correctedGender);
                sb.Append(')');
            }

            // Only add the item if correctedHeldItem is not empty
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

        return line;
    }
}
