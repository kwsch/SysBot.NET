using FuzzySharp;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class NatureHelper<T> where T : PKM, new()
    {
        public static Task<(string? Nature, bool Corrected)> GetClosestNature(string userNature, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            var inputNatures = inputLocalization.Strings.natures;
            var targetNatures = targetLocalization.Strings.natures;

            // First try exact match in input language
            var exactIndex = System.Array.FindIndex(inputNatures, n =>
                !string.IsNullOrEmpty(n) && n.Equals(userNature, System.StringComparison.OrdinalIgnoreCase));

            if (exactIndex >= 0)
            {
                // Found exact match, translate to target language
                var targetNature = exactIndex < targetNatures.Length ? targetNatures[exactIndex] : userNature;
                var corrected = targetNature != userNature;
                return Task.FromResult(((string?)targetNature, corrected));
            }

            // No exact match, try fuzzy matching in input language
            var fuzzyNature = inputNatures
                .Select((nature, index) => new { Nature = nature, Index = index })
                .Where(n => !string.IsNullOrEmpty(n.Nature))
                .Select(n => new {
                    n.Nature,
                    n.Index,
                    Distance = Fuzz.Ratio(userNature, n.Nature)
                })
                .OrderByDescending(n => n.Distance)
                .FirstOrDefault();

            if (fuzzyNature != null && fuzzyNature.Distance >= 80)
            {
                // Found fuzzy match, translate to target language using the index
                var targetNature = fuzzyNature.Index < targetNatures.Length ?
                    targetNatures[fuzzyNature.Index] : fuzzyNature.Nature;
                var corrected = targetNature != userNature;
                return Task.FromResult(((string?)targetNature, corrected));
            }

            // No suitable match found
            return Task.FromResult((null as string, false));
        }
    }
}
