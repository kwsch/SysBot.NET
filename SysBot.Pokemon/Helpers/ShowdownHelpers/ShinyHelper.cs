using PKHeX.Core;
using System;
using System.Collections.Generic;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class ShinyHelper<T> where T : PKM, new()
    {
        public static void VerifyShiny(PKM pk, LegalityAnalysis la, string[] lines, List<string> correctionMessages, string speciesName)
        {
            var enc = la.EncounterMatch;
            if (!enc.Shiny.IsValid(pk))
            {
                correctionMessages.Add($"This encounter of {speciesName} cannot be shiny. Setting to **Shiny: No**.");
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("Shiny: Yes", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = "Shiny: No";
                        break;
                    }
                }
            }
        }
    }
}
