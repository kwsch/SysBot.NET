using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.RibbonIndex;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class MarkHelper<T> where T : PKM, new()
    {
        public static Task<(string? MarkLine, List<string> CorrectionMessages)> CorrectMarks(PKM pk, IEncounterTemplate encounter, string[] lines, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            List<string> correctionMessages = [];

            if (pk is not IRibbonIndex m)
            {
                correctionMessages.Add("PKM does not implement IRibbonIndex. Correcting marks.");
                return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((".Ribbons=$SuggestAll", correctionMessages));
            }

            string? existingMarkLine = lines.FirstOrDefault(line => line.StartsWith(".RibbonMark"));
            if (!string.IsNullOrEmpty(existingMarkLine))
            {
                string markName = existingMarkLine.Split('=')[0].Replace(".RibbonMark", string.Empty);

                var markIndex = TryParseMarkName(markName, inputLocalization);
                if (markIndex.HasValue)
                {
                    if (MarkRules.IsEncounterMarkValid(markIndex.Value, pk, encounter))
                    {
                        m.SetRibbon((int)markIndex.Value, true);
                        string localizedMarkLine = $".RibbonMark{GetLocalizedRibbonName(markIndex.Value, targetLocalization)}=True";
                        return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((localizedMarkLine, correctionMessages));
                    }
                }
            }

            if (MarkRules.IsEncounterMarkAllowed(encounter, pk))
            {
                for (var mark = MarkLunchtime; mark <= MarkSlump; mark++)
                {
                    if (MarkRules.IsEncounterMarkValid(mark, pk, encounter))
                    {
                        m.SetRibbon((int)mark, true);
                        string markLine = $".RibbonMark{GetLocalizedRibbonName(mark, targetLocalization)}=True";
                        return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((markLine, correctionMessages));
                    }
                }
            }

            correctionMessages.Add("Correcting Marks/Ribbons. Changing to **.Ribbons=$SuggestAll**");
            return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((".Ribbons=$SuggestAll", correctionMessages));
        }

        private static RibbonIndex? TryParseMarkName(string markName, BattleTemplateLocalization localization)
        {
            for (var mark = MarkLunchtime; mark <= MarkSlump; mark++)
            {
                var localizedName = GetLocalizedRibbonName(mark, localization);
                if (string.Equals(markName, localizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return mark;
                }
            }

            if (Enum.TryParse($"Mark{markName}", out RibbonIndex markIndex))
            {
                return markIndex;
            }

            return null;
        }

        private static string GetLocalizedRibbonName(RibbonIndex index, BattleTemplateLocalization localization)
        {
            if (index >= MAX_COUNT)
                return index.ToString();

            var ribbonNames = GetRibbonNames(localization);
            var ribbonId = (int)index;

            if (ribbonId < ribbonNames.Length && !string.IsNullOrEmpty(ribbonNames[ribbonId]))
            {
                var ribbonName = ribbonNames[ribbonId];

                if (ribbonName.StartsWith("Ribbon"))
                    return ribbonName["Ribbon".Length..];
                if (ribbonName.StartsWith("Mark"))
                    return ribbonName["Mark".Length..];

                return ribbonName;
            }

            return GetRibbonNameFallback(index);
        }

        private static string[] GetRibbonNames(BattleTemplateLocalization localization)
        {
            var strings = localization.Strings;

            if (strings.ribbons?.Length > 0)
                return strings.ribbons;

            return GetDefaultRibbonNames();
        }

        private static string[] GetDefaultRibbonNames()
        {
            var defaultNames = new string[(int)MAX_COUNT];

            for (int i = 0; i < defaultNames.Length; i++)
            {
                var ribbonIndex = (RibbonIndex)i;
                defaultNames[i] = GetRibbonNameFallback(ribbonIndex);
            }

            return defaultNames;
        }

        private static string GetRibbonNameFallback(RibbonIndex index)
        {
            return index switch
            {
                MarkLunchtime => "Lunchtime",
                MarkSleepyTime => "SleepyTime",
                MarkDusk => "Dusk",
                MarkDawn => "Dawn",
                MarkCloudy => "Cloudy",
                MarkRainy => "Rainy",
                MarkStormy => "Stormy",
                MarkSnowy => "Snowy",
                MarkBlizzard => "Blizzard",
                MarkDry => "Dry",
                MarkSandstorm => "Sandstorm",
                MarkMisty => "Misty",
                MarkDestiny => "Destiny",
                MarkFishing => "Fishing",
                MarkCurry => "Curry",
                MarkUncommon => "Uncommon",
                MarkRare => "Rare",
                MarkRowdy => "Rowdy",
                MarkAbsentMinded => "AbsentMinded",
                MarkJittery => "Jittery",
                MarkExcited => "Excited",
                MarkCharismatic => "Charismatic",
                MarkCalmness => "Calmness",
                MarkIntense => "Intense",
                MarkZonedOut => "ZonedOut",
                MarkJoyful => "Joyful",
                MarkAngry => "Angry",
                MarkSmiley => "Smiley",
                MarkTeary => "Teary",
                MarkUpbeat => "Upbeat",
                MarkPeeved => "Peeved",
                MarkIntellectual => "Intellectual",
                MarkFerocious => "Ferocious",
                MarkCrafty => "Crafty",
                MarkScowling => "Scowling",
                MarkKindly => "Kindly",
                MarkFlustered => "Flustered",
                MarkPumpedUp => "PumpedUp",
                MarkZeroEnergy => "ZeroEnergy",
                MarkPrideful => "Prideful",
                MarkUnsure => "Unsure",
                MarkHumble => "Humble",
                MarkThorny => "Thorny",
                MarkVigor => "Vigor",
                MarkSlump => "Slump",
                _ => index.ToString().Replace("Mark", "").Replace("Ribbon", "")
            };
        }
    }
}
