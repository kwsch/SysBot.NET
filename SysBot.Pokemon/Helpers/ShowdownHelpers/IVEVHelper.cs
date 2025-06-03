using PKHeX.Core;
using System;
using System.Linq;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public interface IVEVHelper<T> where T : PKM, new()
    {
        public static bool ValidateIVs(PKM pk, LegalityAnalysis la, string[] lines, BattleTemplateLocalization localization)
        {
            var ivVerifier = new IndividualValueVerifier();
            ivVerifier.Verify(la);

            return la.Valid;
        }

        public static bool ValidateEVs(string[] lines, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            Span<int> evValues = stackalloc int[6];
            Span<int> correctedEVs = stackalloc int[6];

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var token = inputLocalization.Config.TryParse(line, out var value);

                if (token == BattleTemplateToken.EVs)
                {
                    evValues.Fill(0); // Initialize with zeros

                    var parseResult = inputLocalization.Config.TryParseStats(value, evValues);

                    if (!parseResult.IsParseClean)
                    {
                        return false; // Parsing failed, invalid format
                    }

                    int totalEVs = evValues.ToArray().Sum();

                    if (totalEVs <= EffortValues.Max510)
                    {
                        // EVs are valid, but convert to target language format
                        if (inputLocalization != targetLocalization)
                        {
                            var targetStatDisplay = targetLocalization.Config.GetStatDisplay(StatDisplayStyle.Abbreviated);
                            var correctedEVString = ShowdownSet.GetStringStats(evValues, 0, targetStatDisplay);
                            lines[i] = targetLocalization.Config.Push(BattleTemplateToken.EVs, correctedEVString);
                        }
                        return true;
                    }
                    else
                    {
                        // EVs exceed maximum, correct them proportionally
                        double scaleFactor = (double)EffortValues.Max510 / totalEVs;

                        for (int j = 0; j < evValues.Length; j++)
                        {
                            correctedEVs[j] = (int)Math.Round(evValues[j] * scaleFactor);
                        }

                        // Ensure we don't exceed the limit due to rounding
                        int correctedTotal = correctedEVs.ToArray().Sum();
                        if (correctedTotal > EffortValues.Max510)
                        {
                            // Find the largest EV and reduce it
                            int maxIndex = 0;
                            for (int j = 1; j < correctedEVs.Length; j++)
                            {
                                if (correctedEVs[j] > correctedEVs[maxIndex])
                                    maxIndex = j;
                            }
                            correctedEVs[maxIndex] -= (correctedTotal - EffortValues.Max510);
                        }

                        // Format using target localization
                        var targetStatDisplay = targetLocalization.Config.GetStatDisplay(StatDisplayStyle.Abbreviated);
                        var correctedEVString = ShowdownSet.GetStringStats(correctedEVs, 0, targetStatDisplay);
                        lines[i] = targetLocalization.Config.Push(BattleTemplateToken.EVs, correctedEVString);
                        return true;
                    }
                }
            }

            return true; // No EV line found, consider valid
        }

        public static void RemoveIVLine(string[] lines, BattleTemplateLocalization localization)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var token = localization.Config.TryParse(lines[i], out _);
                if (token == BattleTemplateToken.IVs)
                {
                    // Create new array without the IV line
                    var newLines = new string[lines.Length - 1];
                    Array.Copy(lines, 0, newLines, 0, i);
                    Array.Copy(lines, i + 1, newLines, i, lines.Length - i - 1);

                    // Copy back to original array
                    Array.Resize(ref lines, newLines.Length);
                    Array.Copy(newLines, lines, newLines.Length);
                    break;
                }
            }
        }

        public static void RemoveEVLine(string[] lines, BattleTemplateLocalization localization)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var token = localization.Config.TryParse(lines[i], out _);
                if (token == BattleTemplateToken.EVs)
                {
                    // Create new array without the EV line
                    var newLines = new string[lines.Length - 1];
                    Array.Copy(lines, 0, newLines, 0, i);
                    Array.Copy(lines, i + 1, newLines, i, lines.Length - i - 1);

                    // Copy back to original array
                    Array.Resize(ref lines, newLines.Length);
                    Array.Copy(newLines, lines, newLines.Length);
                    break;
                }
            }
        }

        // Additional helper method for translating existing EV/IV lines to target language
        public static bool TranslateStatsLine(string[] lines, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization, BattleTemplateToken tokenType)
        {
            if (inputLocalization == targetLocalization)
                return true; // No translation needed

            Span<int> statValues = stackalloc int[6];

            for (int i = 0; i < lines.Length; i++)
            {
                var token = inputLocalization.Config.TryParse(lines[i], out var value);
                if (token == tokenType)
                {
                    statValues.Fill(tokenType == BattleTemplateToken.IVs ? 31 : 0);

                    var parseResult = inputLocalization.Config.TryParseStats(value, statValues);
                    if (parseResult.IsParseClean)
                    {
                        var targetStatDisplay = targetLocalization.Config.GetStatDisplay(StatDisplayStyle.Abbreviated);
                        var defaultValue = tokenType == BattleTemplateToken.IVs ? 31 : 0;
                        var translatedStatsString = ShowdownSet.GetStringStats(statValues, defaultValue, targetStatDisplay);
                        lines[i] = targetLocalization.Config.Push(tokenType, translatedStatsString);
                        return true;
                    }
                    return false; // Failed to parse
                }
            }
            return true; // No line found, not an error
        }

        // Convenience method for validating and translating EVs in one call
        public static bool ValidateAndTranslateEVs(string[] lines, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            return ValidateEVs(lines, inputLocalization, targetLocalization);
        }

        // Convenience method for validating IVs and translating if needed
        public static bool ValidateAndTranslateIVs(PKM pk, LegalityAnalysis la, string[] lines, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            bool isValid = ValidateIVs(pk, la, lines, inputLocalization);

            if (isValid && inputLocalization != targetLocalization)
            {
                TranslateStatsLine(lines, inputLocalization, targetLocalization, BattleTemplateToken.IVs);
            }

            return isValid;
        }
    }
}
