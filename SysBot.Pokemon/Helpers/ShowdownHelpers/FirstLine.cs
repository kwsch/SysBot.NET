using FuzzySharp;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class FirstLine<T> where T : PKM, new()
    {
        public static async Task<string?> GetClosestSpecies(string userSpecies, string[]? formNames = null)
        {
            var gameStrings = GameInfoHelpers<T>.GetGameStrings();
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

        public static Task<string?> GetClosestFormName(string userFormName, string[] validFormNames)
        {
            // LogUtil.LogInfo($"Comparing form name: {userFormName}", nameof(AutoCorrectShowdown<T>));

            var (FormName, Distance) = validFormNames
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f => (FormName: f, Distance: Fuzz.Ratio(userFormName, f)))
                .OrderByDescending(f => f.Distance)
                .ThenBy(f => f.FormName.Length)
                .FirstOrDefault();

            // LogUtil.LogInfo($"Closest form name found: {fuzzyFormName.FormName} with distance {fuzzyFormName.Distance}", nameof(AutoCorrectShowdown<T>));

            // Lowered threshold to 70 and added fallback logic
            if (Distance >= 70)
            {
                return Task.FromResult<string?>(FormName);
            }

            // Fallback: return the closest match even if it doesn't meet the threshold
            return Task.FromResult<string?>(FormName);
        }

        public static (string, string) ValidateHeldItem(string[] lines, PKM pk, string[] itemlist, string heldItem)
        {
            if (!string.IsNullOrEmpty(heldItem))
            {
                string correctedHeldItem = GetClosestItem(heldItem, itemlist);
                if (correctedHeldItem != null)
                {
                    int itemIndex = Array.IndexOf(itemlist, correctedHeldItem);
                    if (ItemRestrictions.IsHeldItemAllowed(itemIndex, pk.Context))
                    {
                        if (correctedHeldItem != heldItem)
                        {
                            string correctionMessage = $"Held item was incorrect. Adjusted from **{heldItem}** to **{correctedHeldItem}**.";
                            return (correctedHeldItem, correctionMessage);
                        }
                        return (heldItem, string.Empty); // Item was valid, no correction needed
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
                    return (string.Empty, correctionMessage); // Return empty string for unrecognized item
                }
            }
            return (heldItem, string.Empty); // No item or valid item, no correction needed
        }

        public static string? GetClosestItem(string userItem, string[] itemlist)
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

        public static (string correctedGender, string correctionMessage) ValidateGender(PKM pk, string gender, string speciesName)
        {
            string correctionMessage = string.Empty;
            if (!string.IsNullOrEmpty(gender))
            {
                // Extract the gender value from the parentheses
                string genderValue = gender.Trim('(', ')');

                PersonalInfo personalInfo = pk.PersonalInfo;
                if (personalInfo.Genderless)
                {
                    gender = string.Empty;
                    correctionMessage = $"{speciesName} is genderless. Removing gender.";
                }
                else if ((personalInfo.OnlyFemale && genderValue != "F") || (personalInfo.OnlyMale && genderValue != "M"))
                {
                    gender = string.Empty;
                    correctionMessage = $"{speciesName} can't be {genderValue}. Removing gender.";
                }
            }
            return (gender, correctionMessage);
        }
    }
}
