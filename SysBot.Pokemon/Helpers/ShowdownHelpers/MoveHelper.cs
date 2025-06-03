using FuzzySharp;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.LearnMethod;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Helpers.ShowdownHelpers
{
    public class MoveHelper<T> where T : PKM, new()
    {
        public static async Task ValidateMovesAsync(string[] lines, PKM pk, LegalityAnalysis la, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization, string speciesName, string formName, List<string> correctionMessages)
        {
            var moveLines = lines.Where(line => line.StartsWith("- ")).ToArray();
            var correctedMoveLines = new List<string>();
            var validMoveIds = await GetValidMoveIdsAsync(pk, speciesName, formName, targetLocalization.Strings);
            var validMoveNames = validMoveIds.Select(id => targetLocalization.Strings.movelist[id]).Where(name => !string.IsNullOrEmpty(name)).ToArray();
            var usedMoves = new HashSet<string>();

            for (int i = 0; i < moveLines.Length && i < 4; i++)
            {
                var moveLine = moveLines[i];
                var moveName = moveLine[2..].Trim();
                var correctedMoveName = await GetClosestMoveAsync(moveName, validMoveNames, inputLocalization, targetLocalization);

                if (!string.IsNullOrEmpty(correctedMoveName))
                {
                    if (!usedMoves.Contains(correctedMoveName))
                    {
                        correctedMoveLines.Add($"- {correctedMoveName}");
                        usedMoves.Add(correctedMoveName);
                        if (moveName != correctedMoveName)
                        {
                            correctionMessages.Add($"{speciesName} cannot learn {moveName}. Replaced with **{correctedMoveName}**.");
                        }
                    }
                    else
                    {
                        var unusedValidMoves = validMoveNames.Except(usedMoves).ToList();
                        if (unusedValidMoves.Count > 0)
                        {
                            var randomMove = unusedValidMoves[new Random().Next(unusedValidMoves.Count)];
                            correctedMoveLines.Add($"- {randomMove}");
                            usedMoves.Add(randomMove);
                            correctionMessages.Add($"{speciesName} cannot learn {moveName}. Replaced with **{randomMove}**.");
                        }
                    }
                }
            }

            // Replace the original move lines with the corrected move lines
            for (int i = 0; i < moveLines.Length; i++)
            {
                var moveLine = moveLines[i];
                if (i < correctedMoveLines.Count)
                {
                    lines[Array.IndexOf(lines, moveLine)] = correctedMoveLines[i];
                    LogUtil.LogInfo("ShowdownSet", $"Corrected move: {correctedMoveLines[i]}");
                }
                else
                {
                    lines = lines.Where(line => line != moveLine).ToArray();
                }
            }
        }

        public static async Task<ushort[]> GetValidMoveIdsAsync(PKM pk, string speciesName, string formName, GameStrings gameStrings)
        {
            return await Task.Run(() =>
            {
                var speciesIndex = Array.IndexOf(gameStrings.specieslist, speciesName);
                var form = pk.Form;

                var learnSource = GameInfoHelpers<T>.GetLearnSource(pk);
                var validMoveIds = new List<ushort>();

                if (learnSource is LearnSource9SV learnSource9SV)
                {
                    if (learnSource9SV.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
                    {
                        var evo = new EvoCriteria
                        {
                            Species = (ushort)speciesIndex,
                            Form = form,
                            LevelMax = 100,
                        };

                        // Level-up moves
                        var learnset = learnSource9SV.GetLearnset((ushort)speciesIndex, form);
                        validMoveIds.AddRange(learnset.GetMoveRange(evo.LevelMax));

                        // Egg moves
                        var eggMoves = learnSource9SV.GetEggMoves((ushort)speciesIndex, form);
                        validMoveIds.AddRange(eggMoves);

                        // Reminder moves
                        var reminderMoves = learnSource9SV.GetReminderMoves((ushort)speciesIndex, form);
                        validMoveIds.AddRange(reminderMoves);

                        // TM moves
                        var tmMoves = personalInfo.RecordPermitIndexes.ToArray();
                        validMoveIds.AddRange(tmMoves.Where(m => personalInfo.GetIsLearnTM(Array.IndexOf(tmMoves, m))));
                    }
                }
                else if (learnSource is LearnSource8BDSP learnSource8BDSP)
                {
                    if (learnSource8BDSP.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
                    {
                        var evo = new EvoCriteria
                        {
                            Species = (ushort)speciesIndex,
                            Form = form,
                            LevelMax = 100,
                        };

                        // Level-up moves
                        var learnset = learnSource8BDSP.GetLearnset((ushort)speciesIndex, form);
                        validMoveIds.AddRange(learnset.GetMoveRange(evo.LevelMax));

                        // Egg moves
                        var eggMoves = learnSource8BDSP.GetEggMoves((ushort)speciesIndex, form);
                        validMoveIds.AddRange(eggMoves);

                        var tmMoves = PersonalInfo8BDSP.MachineMoves.ToArray();
                        validMoveIds.AddRange(tmMoves.Where(m => personalInfo.GetIsLearnTM(Array.IndexOf(tmMoves, m))));
                    }
                }
                else if (learnSource is LearnSource8LA learnSource8LA)
                {
                    if (learnSource8LA.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
                    {
                        var evo = new EvoCriteria
                        {
                            Species = (ushort)speciesIndex,
                            Form = form,
                            LevelMax = 100,
                        };

                        // Level-up moves
                        var learnset = learnSource8LA.GetLearnset((ushort)speciesIndex, form);
                        validMoveIds.AddRange(learnset.GetMoveRange(evo.LevelMax));

                        // Move shop (TM) moves
                        var tmMoves = personalInfo.RecordPermitIndexes.ToArray();
                        validMoveIds.AddRange(tmMoves.Where(m => personalInfo.GetIsLearnMoveShop(m)));
                    }
                }
                else if (learnSource is LearnSource8SWSH learnSource8SWSH)
                {
                    if (learnSource8SWSH.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
                    {
                        var evo = new EvoCriteria
                        {
                            Species = (ushort)speciesIndex,
                            Form = form,
                            LevelMax = 100,
                        };

                        // Level-up moves
                        var learnset = learnSource8SWSH.GetLearnset((ushort)speciesIndex, form);
                        validMoveIds.AddRange(learnset.GetMoveRange(evo.LevelMax));

                        // Egg moves
                        var eggMoves = learnSource8SWSH.GetEggMoves((ushort)speciesIndex, form);
                        validMoveIds.AddRange(eggMoves);

                        // TR moves
                        var trMoves = personalInfo.RecordPermitIndexes.ToArray();
                        validMoveIds.AddRange(trMoves.Where(m => personalInfo.GetIsLearnTR(Array.IndexOf(trMoves, m))));
                    }
                }
                else if (learnSource is LearnSource7GG learnSource7GG)
                {
                    if (learnSource7GG.TryGetPersonal((ushort)speciesIndex, form, out var personalInfo))
                    {
                        var evo = new EvoCriteria
                        {
                            Species = (ushort)speciesIndex,
                            Form = form,
                            LevelMax = 100,
                        };

                        // Level-up moves (including Move Reminder)
                        var learnset = learnSource7GG.GetLearnset((ushort)speciesIndex, form);
                        validMoveIds.AddRange(learnset.GetMoveRange(100)); // 100 is the bonus for Move Reminder in LGPE

                        // TM moves and special tutor moves
                        for (ushort move = 0; move < gameStrings.movelist.Length; move++)
                        {
                            var learnInfo = learnSource7GG.GetCanLearn(pk, personalInfo, evo, move);
                            if (learnInfo.Method is TMHM or Tutor)
                            {
                                validMoveIds.Add(move);
                            }
                        }
                    }
                }

                return validMoveIds.Distinct().ToArray();
            });
        }

        public static async Task<string> GetClosestMoveAsync(string userMove, string[] validMoves, BattleTemplateLocalization inputLocalization, BattleTemplateLocalization targetLocalization)
        {
            return await Task.Run(() =>
            {
                // First, try to find exact match in input language and translate to target language
                var inputMoveIndex = Array.FindIndex(inputLocalization.Strings.movelist, m => m.Equals(userMove, StringComparison.OrdinalIgnoreCase));
                if (inputMoveIndex >= 0 && inputMoveIndex < targetLocalization.Strings.movelist.Length)
                {
                    var translatedMoveName = targetLocalization.Strings.movelist[inputMoveIndex];
                    if (!string.IsNullOrEmpty(translatedMoveName) && validMoves.Contains(translatedMoveName))
                    {
                        return translatedMoveName;
                    }
                }

                // If no exact translation match found, try fuzzy matching in target language
                var fuzzyMove = validMoves
                    .Select(m => (Move: m, Distance: Fuzz.Ratio(userMove.ToLower(), m.ToLower())))
                    .OrderByDescending(m => m.Distance)
                    .FirstOrDefault();

                // If the fuzzy match in target language is poor, try fuzzy matching in input language and translate
                if (fuzzyMove.Distance < 80) // Threshold for "good enough" match
                {
                    var inputFuzzyMove = inputLocalization.Strings.movelist
                        .Select((m, index) => (Move: m, Index: index, Distance: Fuzz.Ratio(userMove.ToLower(), m.ToLower())))
                        .Where(m => !string.IsNullOrEmpty(m.Move))
                        .OrderByDescending(m => m.Distance)
                        .FirstOrDefault();

                    if (inputFuzzyMove.Distance > fuzzyMove.Distance && inputFuzzyMove.Index < targetLocalization.Strings.movelist.Length)
                    {
                        var translatedMove = targetLocalization.Strings.movelist[inputFuzzyMove.Index];
                        if (!string.IsNullOrEmpty(translatedMove) && validMoves.Contains(translatedMove))
                        {
                            return translatedMove;
                        }
                    }
                }

                return fuzzyMove.Move;
            });
        }
    }
}
