using FuzzySharp;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class NatureHelper<T> where T : PKM, new()
    {
        public static Task<(string? Nature, bool Corrected)> GetClosestNature(string userNature, GameStrings gameStrings)
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
    }
}
