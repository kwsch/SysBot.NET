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
        public static Task<(string? MarkLine, List<string> CorrectionMessages)> CorrectMarks(PKM pk, IEncounterTemplate encounter, string[] lines)
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
                if (Enum.TryParse($"Mark{markName}", out RibbonIndex markIndex))
                {
                    if (MarkRules.IsEncounterMarkValid(markIndex, pk, encounter))
                    {
                        m.SetRibbon((int)markIndex, true);
                        return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((existingMarkLine, correctionMessages));
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
                        string markLine = $".RibbonMark{GetRibbonNameSafe(mark)}=True";
                        return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((markLine, correctionMessages));
                    }
                }
            }

            correctionMessages.Add("Correcting Marks/Ribbons.  Changing to **.Ribbons=$SuggestAll**");
            return Task.FromResult<(string? MarkLine, List<string> CorrectionMessages)>((".Ribbons=$SuggestAll", correctionMessages));
        }

        public static string GetRibbonNameSafe(RibbonIndex index)
        {
            if (index >= MAX_COUNT)
                return index.ToString();
            var expect = $"Ribbon{index}";
            return RibbonStrings.GetName(expect);
        }
    }
}
