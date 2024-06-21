using PKHeX.Core;
using System;
using System.Linq;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public interface IVEVHelper<T> where T : PKM, new()
    {
        public static bool ValidateIVs(PKM pk, LegalityAnalysis la, string[] lines)
        {
            var ivVerifier = new IndividualValueVerifier();
            ivVerifier.Verify(la);

            return la.Valid;
        }

        public static bool ValidateEVs(string[] lines)
        {
            int totalEVs = 0;
            string originalEVs = string.Empty;
            string modifiedEVs = string.Empty;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("EVs:"))
                {
                    originalEVs = line;
                    string[] evParts = line.Replace("EVs:", "").Split('/');
                    int[] evValues = new int[6];
                    string[] statNames = ["HP", "Atk", "Def", "SpA", "SpD", "Spe"];

                    foreach (string evPart in evParts)
                    {
                        string[] evValue = evPart.Trim().Split(' ');
                        if (evValue.Length == 2 && int.TryParse(evValue[0], out int value))
                        {
                            string statName = evValue[1];
                            int statIndex = Array.IndexOf(statNames, statName);
                            if (statIndex >= 0)
                            {
                                evValues[statIndex] = value;
                                totalEVs += value;
                            }
                        }
                    }

                    if (totalEVs <= EffortValues.Max510)
                    {
                        return true;
                    }
                    else
                    {
                        // EVs exceed the maximum, correct them proportionally
                        double scaleFactor = (double)EffortValues.Max510 / totalEVs;
                        for (int j = 0; j < evValues.Length; j++)
                        {
                            evValues[j] = (int)Math.Round(evValues[j] * scaleFactor);
                        }

                        modifiedEVs = "EVs: " + string.Join(" / ", evValues.Select((ev, index) => $"{ev} {statNames[index]}"));
                        lines[i] = modifiedEVs;
                        return true;
                    }
                }
            }

            return true;
        }

        public static void RemoveIVLine(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("IVs:"))
                {
                    lines = lines.Where((line, index) => index != i).ToArray();
                    break;
                }
            }
        }

        public static void RemoveEVLine(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("EVs:"))
                {
                    lines = lines.Where((line, index) => index != i).ToArray();
                    break;
                }
            }
        }
    }
}
