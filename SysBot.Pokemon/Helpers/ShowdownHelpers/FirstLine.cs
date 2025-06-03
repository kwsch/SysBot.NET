using FuzzySharp;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class FirstLine<T> where T : PKM, new()
    {
        public static async Task<string?> GetClosestSpecies(string userSpecies, BattleTemplateLocalization? inputLoc = null, BattleTemplateLocalization? targetLoc = null, string[]? formNames = null)
        {
            inputLoc ??= BattleTemplateLocalization.Default;
            targetLoc ??= BattleTemplateLocalization.Default;

            var speciesName = userSpecies.Split('-')[0].Trim();
            var formNamePart = userSpecies.Contains('-') ? string.Join("-", userSpecies.Split('-').Skip(1)).Trim() : string.Empty;

            // Try exact match in target language first
            var exactMatch = Array.FindIndex(targetLoc.Strings.specieslist, s => s.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch > 0)
            {
                var correctedSpecies = targetLoc.Strings.specieslist[exactMatch];
                return await HandleFormName(correctedSpecies, formNamePart, formNames, inputLoc, targetLoc);
            }

            // Try fuzzy match in target language
            var targetFuzzyMatch = GetBestFuzzyMatch(speciesName, targetLoc.Strings.specieslist, 80);
            if (targetFuzzyMatch != null)
            {
                return await HandleFormName(targetFuzzyMatch, formNamePart, formNames, inputLoc, targetLoc);
            }

            // Try to find in input language and translate to target
            if (inputLoc != targetLoc)
            {
                var inputMatch = Array.FindIndex(inputLoc.Strings.specieslist, s => s.Equals(speciesName, StringComparison.OrdinalIgnoreCase));
                if (inputMatch > 0 && inputMatch < targetLoc.Strings.specieslist.Length)
                {
                    var translatedSpecies = targetLoc.Strings.specieslist[inputMatch];
                    return await HandleFormName(translatedSpecies, formNamePart, formNames, inputLoc, targetLoc);
                }

                // Try fuzzy match in input language and translate
                var inputFuzzyMatch = GetBestFuzzyMatch(speciesName, inputLoc.Strings.specieslist, 80);
                if (inputFuzzyMatch != null)
                {
                    var inputIndex = Array.IndexOf(inputLoc.Strings.specieslist, inputFuzzyMatch);
                    if (inputIndex > 0 && inputIndex < targetLoc.Strings.specieslist.Length)
                    {
                        var translatedSpecies = targetLoc.Strings.specieslist[inputIndex];
                        return await HandleFormName(translatedSpecies, formNamePart, formNames, inputLoc, targetLoc);
                    }
                }
            }

            return null;
        }

        private static async Task<string> HandleFormName(string correctedSpecies, string formNamePart, string[]? formNames, BattleTemplateLocalization inputLoc, BattleTemplateLocalization targetLoc)
        {
            if (!string.IsNullOrEmpty(formNamePart) && formNames != null)
            {
                var closestFormName = await GetClosestFormName(formNamePart, formNames, inputLoc, targetLoc);
                if (closestFormName != null)
                {
                    return $"{correctedSpecies}-{closestFormName}";
                }
                return $"{correctedSpecies}-{formNamePart}";
            }
            return correctedSpecies;
        }

        public static Task<string?> GetClosestFormName(string userFormName, string[] validFormNames, BattleTemplateLocalization? inputLoc = null, BattleTemplateLocalization? targetLoc = null)
        {
            inputLoc ??= BattleTemplateLocalization.Default;
            targetLoc ??= BattleTemplateLocalization.Default;

            // Try exact match first
            var exactMatch = validFormNames.FirstOrDefault(f => f.Equals(userFormName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return Task.FromResult<string?>(exactMatch);

            // Try fuzzy match
            var fuzzyMatch = GetBestFuzzyMatch(userFormName, validFormNames, 70);
            if (fuzzyMatch != null)
                return Task.FromResult<string?>(fuzzyMatch);

            // Forms are species-specific and generated dynamically, so cross-language translation
            // is more complex and handled by PKHeX's FormConverter system
            // Return the best match even if below threshold
            var fallbackMatch = validFormNames
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f => (FormName: f, Distance: Fuzz.Ratio(userFormName, f)))
                .OrderByDescending(f => f.Distance)
                .ThenBy(f => f.FormName.Length)
                .FirstOrDefault().FormName;

            return Task.FromResult<string?>(fallbackMatch);
        }

        public static (string, string) ValidateHeldItem(string[] lines, PKM pk, BattleTemplateLocalization inputLoc, BattleTemplateLocalization targetLoc, string heldItem)
        {
            if (!string.IsNullOrEmpty(heldItem))
            {
                string? correctedHeldItem = GetClosestItem(heldItem, inputLoc, targetLoc, pk.Context);
                if (correctedHeldItem != null)
                {
                    var targetItemList = targetLoc.Strings.GetItemStrings(pk.Context);
                    int itemIndex = Array.IndexOf(targetItemList, correctedHeldItem);
                    if (ItemRestrictions.IsHeldItemAllowed(itemIndex, pk.Context))
                    {
                        if (correctedHeldItem != heldItem)
                        {
                            string correctionMessage = $"Held item was incorrect. Adjusted from **{heldItem}** to **{correctedHeldItem}**.";
                            return (correctedHeldItem, correctionMessage);
                        }
                        return (heldItem, string.Empty);
                    }
                    else
                    {
                        string correctionMessage = $"Held item **{heldItem}** is not allowed. Removed the held item.";
                        return (string.Empty, correctionMessage);
                    }
                }
                else
                {
                    string correctionMessage = $"Held item **{heldItem}** is not recognized. Removed the held item.";
                    return (string.Empty, correctionMessage);
                }
            }
            return (heldItem, string.Empty);
        }

        public static string? GetClosestItem(string userItem, BattleTemplateLocalization inputLoc, BattleTemplateLocalization targetLoc, EntityContext context)
        {
            var targetItemList = targetLoc.Strings.GetItemStrings(context);

            // Try exact match in target language first
            var exactMatch = Array.FindIndex(targetItemList, item => item.Equals(userItem, StringComparison.OrdinalIgnoreCase));
            if (exactMatch > 0)
                return targetItemList[exactMatch];

            // Try fuzzy match in target language
            var targetFuzzyMatch = GetBestFuzzyMatch(userItem, targetItemList, 80);
            if (targetFuzzyMatch != null)
                return targetFuzzyMatch;

            // Try to find in input language and translate to target
            if (inputLoc != targetLoc)
            {
                var inputItemList = inputLoc.Strings.GetItemStrings(context);
                var inputMatch = Array.FindIndex(inputItemList, item => item.Equals(userItem, StringComparison.OrdinalIgnoreCase));
                if (inputMatch > 0 && inputMatch < targetItemList.Length)
                    return targetItemList[inputMatch];

                // Try fuzzy match in input language and translate
                var inputFuzzyMatch = GetBestFuzzyMatch(userItem, inputItemList, 80);
                if (inputFuzzyMatch != null)
                {
                    var inputIndex = Array.IndexOf(inputItemList, inputFuzzyMatch);
                    if (inputIndex > 0 && inputIndex < targetItemList.Length)
                        return targetItemList[inputIndex];
                }
            }

            return null;
        }

        public static (string correctedGender, string correctionMessage) ValidateGender(PKM pk, string gender, string speciesName, BattleTemplateLocalization? inputLoc = null, BattleTemplateLocalization? targetLoc = null)
        {
            inputLoc ??= BattleTemplateLocalization.Default;
            _ = targetLoc ?? BattleTemplateLocalization.Default;

            string correctionMessage = string.Empty;
            string correctedGender = gender;

            if (!string.IsNullOrEmpty(gender))
            {
                PersonalInfo personalInfo = pk.PersonalInfo;

                // Normalize gender to M/F format
                string normalizedGender = NormalizeGender(gender, inputLoc);

                // First check if the species is genderless
                if (personalInfo.Genderless)
                {
                    correctedGender = string.Empty;
                    correctionMessage = $"{speciesName} is genderless. Removing gender.";
                }
                // Check if only female and gender is male
                else if (personalInfo.OnlyFemale && normalizedGender == "M")
                {
                    correctedGender = string.Empty;
                    correctionMessage = $"{speciesName} can only be female. Removing gender.";
                }
                // Check if only male and gender is female
                else if (personalInfo.OnlyMale && normalizedGender == "F")
                {
                    correctedGender = string.Empty;
                    correctionMessage = $"{speciesName} can only be male. Removing gender.";
                }
                else
                {
                    // Return normalized M/F format
                    correctedGender = normalizedGender;
                }
            }

            return (correctedGender, correctionMessage);
        }

        private static string NormalizeGender(string gender, BattleTemplateLocalization localization)
        {
            // Check if it's already M or F
            if (gender.Equals("M", StringComparison.OrdinalIgnoreCase))
                return "M";
            if (gender.Equals("F", StringComparison.OrdinalIgnoreCase))
                return "F";

            // Check against localized strings
            if (gender.Equals(localization.Config.Male, StringComparison.OrdinalIgnoreCase))
                return "M";
            if (gender.Equals(localization.Config.Female, StringComparison.OrdinalIgnoreCase))
                return "F";

            // Default return original if can't normalize
            return gender;
        }

        private static string? GetBestFuzzyMatch(string input, string[] candidates, int threshold)
        {
            var (Item, Distance) = candidates
                .Where(candidate => !string.IsNullOrEmpty(candidate))
                .Select(candidate => (Item: candidate, Distance: Fuzz.Ratio(input.ToLowerInvariant(), candidate.ToLowerInvariant())))
                .OrderByDescending(x => x.Distance)
                .FirstOrDefault();

            return Distance >= threshold ? Item : null;
        }
    }
}
