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
        public static async Task ValidateMovesAsync(string[] lines, PKM pk, LegalityAnalysis la, GameStrings gameStrings, string speciesName, string formName, List<string> correctionMessages)
        {
            var moveLines = lines.Where(line => line.StartsWith("- ")).ToArray();
            var correctedMoveLines = new List<string>();
            var validMoves = await GetValidMovesAsync(pk, gameStrings, speciesName, formName);
            var usedMoves = new HashSet<string>();
            for (int i = 0; i < moveLines.Length && i < 4; i++)
            {
                var moveLine = moveLines[i];
                var moveName = moveLine[2..].Trim();
                var correctedMoveName = await GetClosestMoveAsync(moveName, validMoves);
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
                        var unusedValidMoves = validMoves.Except(usedMoves).ToList();
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

        public static async Task<string[]> GetValidMovesAsync(PKM pk, GameStrings gameStrings, string speciesName, string formName)
        {
            return await Task.Run(() =>
            {
                var speciesIndex = Array.IndexOf(gameStrings.specieslist, speciesName);
                var form = pk.Form;

                var learnSource = GameInfoHelpers<T>.GetLearnSource(pk);
                var validMoves = new List<string>();

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
                        validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // Egg moves
                        var eggMoves = learnSource9SV.GetEggMoves((ushort)speciesIndex, form).ToArray();
                        validMoves.AddRange(eggMoves
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // Reminder moves
                        var reminderMoves = learnSource9SV.GetReminderMoves((ushort)speciesIndex, form).ToArray();
                        validMoves.AddRange(reminderMoves
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // TM moves
                        var tmMoves = personalInfo.RecordPermitIndexes.ToArray();
                        validMoves.AddRange(tmMoves
                            .Where(m => personalInfo.GetIsLearnTM(Array.IndexOf(tmMoves, m)))
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));
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
                        validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // Egg moves
                        var eggMoves = learnSource8BDSP.GetEggMoves((ushort)speciesIndex, form).ToArray();
                        validMoves.AddRange(eggMoves
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // TM moves
                        var tmMoves = PersonalInfo8BDSP.MachineMoves.ToArray();
                        validMoves.AddRange(tmMoves
                            .Where(m => personalInfo.GetIsLearnTM(Array.IndexOf(tmMoves, m)))
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));
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
                        validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // Move shop (TM) moves
                        var tmMoves = personalInfo.RecordPermitIndexes.ToArray();
                        validMoves.AddRange(tmMoves
                            .Where(m => personalInfo.GetIsLearnMoveShop(m))
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));
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
                        validMoves.AddRange(learnset.GetMoveRange(evo.LevelMax).ToArray()
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // Egg moves
                        var eggMoves = learnSource8SWSH.GetEggMoves((ushort)speciesIndex, form).ToArray();
                        validMoves.AddRange(eggMoves
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // TR moves
                        var trMoves = personalInfo.RecordPermitIndexes.ToArray();
                        validMoves.AddRange(trMoves
                            .Where(m => personalInfo.GetIsLearnTR(Array.IndexOf(trMoves, m)))
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));
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
                        validMoves.AddRange(learnset.GetMoveRange(100).ToArray() // 100 is the bonus for Move Reminder in LGPE
                            .Select(m => gameStrings.movelist[m])
                            .Where(m => !string.IsNullOrEmpty(m)));

                        // TM moves and special tutor moves
                        for (int move = 0; move < gameStrings.movelist.Length; move++)
                        {
                            var learnInfo = learnSource7GG.GetCanLearn(pk, personalInfo, evo, (ushort)move);
                            if (learnInfo.Method is TMHM or Tutor)
                            {
                                var moveName = gameStrings.movelist[move];
                                if (!string.IsNullOrEmpty(moveName))
                                    validMoves.Add(moveName);
                            }
                        }
                    }
                }
                return validMoves.Distinct().ToArray();
            });
        }

        public static async Task<string> GetClosestMoveAsync(string userMove, string[] validMoves)
        {
            return await Task.Run(() =>
            {
                // LogUtil.LogInfo($"User move: {userMove}", nameof(AutoCorrectShowdown<T>)); // Debug Stuff
                // LogUtil.LogInfo($"Valid moves: {string.Join(", ", validMoves)}", nameof(AutoCorrectShowdown<T>)); // Debug Stuff

                var fuzzyMove = validMoves
                .Select(m => (Move: m, Distance: Fuzz.Ratio(userMove.ToLower(), m.ToLower())))
                .OrderByDescending(m => m.Distance)
                .FirstOrDefault();

                // LogUtil.LogInfo($"Closest move: {fuzzyMove.Move}, Distance: {fuzzyMove.Distance}", nameof(AutoCorrectShowdown<T>)); // Debug Stuff

                return fuzzyMove.Move;
            });
        }

    }
}
