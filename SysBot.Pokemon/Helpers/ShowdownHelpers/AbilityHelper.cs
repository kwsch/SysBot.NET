using FuzzySharp;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class AbilityHelper<T> where T : PKM, new()
    {
        public static Task<(string? Ability, bool Corrected)> GetClosestAbility(string userAbility, ushort speciesIndex, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization, IPersonalAbility12 personalInfo)
        {
            return GetClosestAbility(userAbility, speciesIndex, inputLocalization.Strings, targetLocalization.Strings, personalInfo);
        }

        public static Task<(string? Ability, bool Corrected)> GetClosestAbility(string userAbility, ushort speciesIndex, GameStrings inputStrings, GameStrings targetStrings, IPersonalAbility12 personalInfo)
        {
            if (string.IsNullOrEmpty(userAbility))
                return Task.FromResult<(string? Ability, bool Corrected)>((null, false));

            var validAbilityIndices = Enumerable.Range(0, personalInfo.AbilityCount)
                .Select(i => personalInfo.GetAbilityAtIndex(i))
                .Where(index => index > 0 && index < targetStrings.abilitylist.Length)
                .ToList();

            if (validAbilityIndices.Count == 0)
                return Task.FromResult<(string? Ability, bool Corrected)>((null, false));

            var targetAbilities = validAbilityIndices
                .Select(index => targetStrings.abilitylist[index])
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList();

            // Step 1: Try exact match in target language
            var exactMatch = targetAbilities.FirstOrDefault(a => string.Equals(a, userAbility, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return Task.FromResult((exactMatch, false));

            // Step 2: Try exact match in input language, then translate to target
            if (inputStrings != targetStrings)
            {
                var inputAbilities = validAbilityIndices
                    .Select(index => inputStrings.abilitylist[index])
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();

                var inputExactMatch = inputAbilities.FirstOrDefault(a => string.Equals(a, userAbility, StringComparison.OrdinalIgnoreCase));
                if (inputExactMatch != null)
                {
                    var inputIndex = Array.IndexOf(inputStrings.abilitylist, inputExactMatch);
                    if (inputIndex >= 0 && inputIndex < targetStrings.abilitylist.Length)
                    {
                        var translatedAbility = targetStrings.abilitylist[inputIndex];
                        if (!string.IsNullOrEmpty(translatedAbility))
                            return Task.FromResult((translatedAbility, true));
                    }
                }
            }

            // Step 3: Fuzzy match in target language
            var targetFuzzyMatch = targetAbilities
                .Select(a => (Ability: a, Distance: Fuzz.Ratio(userAbility, a)))
                .OrderByDescending(a => a.Distance)
                .FirstOrDefault();

            if (targetFuzzyMatch.Distance >= 60)
                return Task.FromResult((targetFuzzyMatch.Ability, true));

            // Step 4: Fuzzy match in input language, then translate to target
            if (inputStrings != targetStrings)
            {
                var inputAbilities = validAbilityIndices
                    .Select(index => inputStrings.abilitylist[index])
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();

                var inputFuzzyMatch = inputAbilities
                    .Select(a => (Ability: a, Distance: Fuzz.Ratio(userAbility, a)))
                    .OrderByDescending(a => a.Distance)
                    .FirstOrDefault();

                if (inputFuzzyMatch.Distance >= 60)
                {
                    var inputIndex = Array.IndexOf(inputStrings.abilitylist, inputFuzzyMatch.Ability);
                    if (inputIndex >= 0 && inputIndex < targetStrings.abilitylist.Length)
                    {
                        var translatedAbility = targetStrings.abilitylist[inputIndex];
                        if (!string.IsNullOrEmpty(translatedAbility))
                            return Task.FromResult((translatedAbility, true));
                    }
                }
            }

            // Step 5: Fallback to random valid ability in target language
            var fallbackAbility = targetAbilities[new Random().Next(targetAbilities.Count)];
            return Task.FromResult((fallbackAbility, true));
        }
    }
}
