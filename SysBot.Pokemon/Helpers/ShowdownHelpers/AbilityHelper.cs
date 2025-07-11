using FuzzySharp;
using PKHeX.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class AbilityHelper<T> where T : PKM, new()
    {
        public static Task<(string? Ability, bool Corrected)> GetClosestAbility(string userAbility, ushort speciesIndex, GameStrings gameStrings, IPersonalAbility12 personalInfo)
        {
            var abilities = Enumerable.Range(0, personalInfo.AbilityCount)
                .Select(i => gameStrings.abilitylist[personalInfo.GetAbilityAtIndex(i)])
                .Where(a => !string.IsNullOrEmpty(a))
                .ToList();

            // LogUtil.LogInfo($"Extracted abilities for species index {speciesIndex}: {string.Join(", ", abilities)}", nameof(GetClosestAbility));

            if (string.IsNullOrEmpty(userAbility))
            {
                // LogUtil.LogInfo("User-provided ability is null or empty. No correction needed.", nameof(GetClosestAbility));
                return Task.FromResult<(string? Ability, bool Corrected)>((null, false));
            }

            // LogUtil.LogInfo($"User-provided ability: {userAbility}", nameof(GetClosestAbility));

            var fuzzyAbility = abilities
                .Select(a => (Ability: a, Distance: Fuzz.Ratio(userAbility, a)))
                .OrderByDescending(a => a.Distance)
                .FirstOrDefault();

            // LogUtil.LogInfo($"Closest matching ability: {fuzzyAbility.Ability} (Distance: {fuzzyAbility.Distance})", nameof(GetClosestAbility));

            var correctedAbility = fuzzyAbility.Distance >= 60 ? fuzzyAbility.Ability : null;

            if (correctedAbility == null)
            {
                // If no closest match is found, fallback to a random valid ability
                correctedAbility = abilities[new Random().Next(abilities.Count)];
            }

            var corrected = correctedAbility != null && !string.Equals(correctedAbility, userAbility, StringComparison.OrdinalIgnoreCase);

            //if (corrected)
            //{
            //    LogUtil.LogInfo($"Ability corrected from '{userAbility}' to '{correctedAbility}'", nameof(GetClosestAbility));
            //}
            //else
            //{
            //    LogUtil.LogInfo($"No ability correction needed. User-provided ability '{userAbility}' is valid.", nameof(GetClosestAbility));
            //}

            return Task.FromResult((correctedAbility, corrected));
        }
    }
}
