using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using PKHeX.Core;

namespace SysBot.Pokemon.Discord.Helpers
{ 
    /// <summary>
    /// The Batch Commands to be converted into standard Showdown format. ///
    /// </summary>
    public static class BatchNormalizer
    {
        // RNG for all handlers (avoid creating new ones in loops)
        private static readonly Random Rng = new();

        //////////////////////////////////// ALIAS & COMMAND MAPPINGS //////////////////////////////////////

        private static readonly Dictionary<string, string> BatchCommandAliasMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // New Showdown format inputs to override Batch commands that start with a period(.) //
            { "Size", "Scale" },
            { "Weight", "WeightScalar" },
            { "Height", "HeightScalar" },
            { "Met Date", "MetDate" },
            { "Egg Met Date", "EggDate" },
            { "Met Location", "MetLocation" },
            { "Game", "Version" },
            { "Hypertrain", "HyperTrainFlags" },
            { "Moves", "Moves" },
            { "Relearn Moves", "RelearnMoves" },
            { "Met Level", "MetLevel" },
            { "Ribbons", "Ribbons" },
            { "Mark", "Mark" },
            { "Ribbon", "Ribbon" },
            { "GVs", "GVs" },
            { "EVs", "EVs" },
            { "IVs", "IVs" },
            { "OT Friendship", "OriginalTrainerFriendship" },
            { "HT Friendship", "HandlingTrainerFriendship" },
        };

        // Core mapping of functions for each key
        private static readonly Dictionary<string, Func<string, string>> CommandProcessors =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "Scale", ProcessScale },
            { "WeightScalar", ProcessWeightScalar },
            { "HeightScalar", ProcessHeightScalar },
            { "OriginalTrainerFriendship", ProcessFriendshipOT },
            { "HandlingTrainerFriendship", ProcessFriendshipHT },
            { "MetDate", val => ProcessDate("MetDate", val) },
            { "EggDate", ProcessEggMetDate },
            { "Version", ProcessVersion },
            { "MetLocation", ProcessMetLocation },
            { "HyperTrainFlags", ProcessHyperTrainFlags },
            { "Moves", ProcessMoves },
            { "RelearnMoves", ProcessRelearnMoves },
            { "Ribbons", ProcessRibbons },
            { "Mark", ProcessMark },
            { "Ribbon", ProcessRibbon },
            { "GVs", ProcessGVs },
            { "EVs", ProcessEVs },
            { "IVs", ProcessIVs }
        };

        //////////////////////////////////// NEW COMMAND DICTIONARIES //////////////////////////////////////

        private static readonly Dictionary<string, (int Min, int Max)> SizeKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            { "XXXS", (0, 0) }, { "XXS", (1, 30) }, { "XS", (31, 60) }, { "S", (61, 100) },
            { "AV", (101, 160) }, { "L", (161, 195) }, { "XL", (196, 241) }, { "XXL", (242, 254) }, { "XXXL", (255, 255) }
        };

        private static readonly Dictionary<string, (int Min, int Max)> ScalarKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            { "XS", (0, 15) }, { "S", (16, 47) }, { "AV", (48, 207) }, { "L", (208, 239) }, { "XL", (240, 255) }
        };

        private static readonly string[] AcceptedDateFormats =
        {
            "yyyyMMdd", "MMddyyyy", "yyyy/MM/dd", "MM/dd/yyyy", "yyyy-MM-dd", "MM-dd-yyyy"
        };

        private static readonly Dictionary<string, int> GameKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Red", 35 }, { "Blue", 36 }, { "Green", 36 }, { "BlueJP", 37 }, { "Yellow", 38 },
            { "Gold", 39 }, { "Silver", 40 }, { "Crystal", 41 }, { "Sapphire", 1 }, { "Ruby", 2 },
            { "Emerald", 3 }, { "FR", 4 }, { "Fire Red", 4 }, { "LG", 5 }, { "Leaf Green", 5 },
            { "Colosseum", 15 }, { "XD", 15 }, { "HG", 7 }, { "Heart Gold", 7 }, { "SS", 8 }, { "Soul Silver", 8 },
            { "Diamond", 10 }, { "D", 10 }, { "Pearl", 11 }, { "P", 11 }, { "Platinum", 12 }, { "Pt", 12 },
            { "B", 21 }, { "Black", 21 }, { "B2", 23 }, { "Black 2", 23 }, { "W", 20 }, { "White", 20 },
            { "W2", 22 }, { "White 2", 22 }, { "X", 24 }, { "Y", 25 }, { "AS", 26 }, { "Alpha Sapphire", 26 },
            { "OR", 27 }, { "Omega Ruby", 27 }, { "S", 30 }, { "Sun", 30 }, { "M", 31 }, { "Moon", 31 },
            { "US", 32 }, { "Ultra Sun", 32 }, { "UM", 33 }, { "Ultra Moon", 33 },
            { "Pikachu", 42 }, { "LetsGoPikachu", 42 }, { "LGP", 42 }, { "Eevee", 43 }, { "LetsGoEevee", 43 }, { "LGE", 43 },
            { "GO", 34 }, { "Pokemon GO", 34 }, { "SW", 44 }, { "Sword", 44 }, { "SH", 45 }, { "Shield", 45 },
            { "PLA", 47 }, { "Legends Arceus", 47 }, { "BD", 48 }, { "Brilliant Diamond", 48 },
            { "SP", 49 }, { "Shining Pearl", 49 }, { "Scarlet", 50 }, { "SL", 50 }, { "Violet", 51 }, { "VL", 51 }
        };

        //////////////////////////////////// MAIN ENTRY //////////////////////////////////////

        public static string NormalizeBatchCommands(string content)
        {
            // Special-case handling for Alcremie toppings
            content = HandleAlcremieToppings(content);

            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (!TrySplitCommand(lines[i], out var key, out var value))
                    continue;

                // Normalize alias
                if (BatchCommandAliasMap.TryGetValue(key, out var normalizedKey))
                    key = normalizedKey;

                // Process with handler
                if (CommandProcessors.TryGetValue(key, out var processor))
                {
                    lines[i] = processor(value);
                }
            
            }

            return string.Join('\n', lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }

        //////////////////////////////////// HANDLER METHODS //////////////////////////////////////

        // .Scale= → Size: or Scale:
        // Value is a size keyword or number 
        private static string ProcessScale(string val) =>
            SizeKeywords.TryGetValue(val, out var range)
                ? $".Scale={Rng.Next(range.Min, range.Max + 1)}"
                : $".Scale={val}";

        // .WeightScalar= → Weight:
        // Value is a size keyword or number 
        private static string ProcessWeightScalar(string val) =>
            ScalarKeywords.TryGetValue(val, out var range)
                ? $".WeightScalar={Rng.Next(range.Min, range.Max + 1)}"
                : $".WeightScalar={val}";

        // HeightScalar= → Height:
        // Value is a size keyword or number
        private static string ProcessHeightScalar(string val) =>
            ScalarKeywords.TryGetValue(val, out var range)
                ? $".HeightScalar={Rng.Next(range.Min, range.Max + 1)}"
                : $".HeightScalar={val}";

        // .OriginalTrainerFriendship= → OT Friendship:
        // Value is between 1-255
        private static string ProcessFriendshipOT(string val) =>
            int.TryParse(val, out int f) && f >= 1 && f <= 255
                ? $".OriginalTrainerFriendship={f}"
                : string.Empty;

        // .HandlingTrainerFriendship= → HT Friendship:
        // Value is between 1-255
        private static string ProcessFriendshipHT(string val) =>
            int.TryParse(val, out int f) && f >= 1 && f <= 255
                ? $".HandlingTrainerFriendship={f}"
                : string.Empty;

        // .MetDate= → Met Date:
        // See the AcceptedDateFormats dictionary
        private static string ProcessDate(string key, string val) =>
            TryParseFlexibleDate(val, out var formatted)
                ? $".{key}={formatted}"
                : string.Empty;

        // Converts .EggDay=, .EggMonth=, & .EggYear= into a single .EggDate= line that acccepts "Egg Met Date:" as a command
        // It accepts the same date formats in the AccceptedDateFormats dictionary
        private static string ProcessEggMetDate(string val)
        {
            if (TryParseFlexibleDate(val, out var formatted))
            {
                int year = int.Parse(formatted.Substring(0, 4));
                int month = int.Parse(formatted.Substring(4, 2));
                int day = int.Parse(formatted.Substring(6, 2));
                int eggYear = year % 100;

                return $".EggMonth={month}\n.EggDay={day}\n.EggYear={eggYear}";
            }
            return string.Empty;
        }

        // ~=Version= → Game: or Version:
        // Value is game name or game abbreviation
        private static string ProcessVersion(string val) =>
            GameKeywords.TryGetValue(val.Replace(" ", ""), out int ver)
                ? $"~=Version={ver}"
                : string.Empty;

        // .MetLocation= → Met Location:
        // Value is a numeric value only for now
        private static string ProcessMetLocation(string val) =>
            $".MetLocation={val}"; // Hook into your met location resolver here

        // .HyperTrainFlags= → HyperTrain: or HyperTrainFlags:
        // Value is "true" or "false"
        private static string ProcessHyperTrainFlags(string val) =>
            TryParseBoolean(val, out var b)
                ? $".HyperTrainFlags={b}"
                : string.Empty;

        // .Moves= → Moves:
        // Only accepted options are "Random" for randomized moves
        private static string ProcessMoves(string val) =>
            val.Equals("Random", StringComparison.OrdinalIgnoreCase)
                ? ".Moves=$suggest"
                : $".Moves={val}";

        // .RelearnMoves= → Relearn Moves:
        // Only accepted options are "All" or "None"
        private static string ProcessRelearnMoves(string value)
        {
            // trim and normalize spacing
            value = value.Trim();

            if (value.Equals("All", StringComparison.OrdinalIgnoreCase))
                return ".RelearnMoves=$suggestAll";

            if (value.Equals("None", StringComparison.OrdinalIgnoreCase))
                return ".RelearnMoves=$suggestNone";

            // fallback in case user typed something weird
            return $".RelearnMoves={value}";
        }

        // .Ribbons= → Ribbons:
        // Only accepted options are "All" or "None"
        private static string ProcessRibbons(string val) =>
            val.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? ".Ribbons=$suggestAll"
                : $".Ribbons={val}";

        // .RibbonMark[mark]=True → Mark:
        // Value is a mark name like "BestFriends," without spaces
        private static string ProcessMark(string val) =>
            $".RibbonMark{val.Replace(" ", "")}=True";

        // .Ribbon[name]= → Ribbon:
        // Value is a Ribbon name like "BattleChampion," without using spaces
        private static string ProcessRibbon(string val) =>
            $".Ribbon{val.Replace(" ", "")}=True";

        // Creates an ".EVs=" batch command that can be written as "EVs:" that accepts "Random" or "Suggest" as special values
        // "EVs: Random" value randomizes EVs across all stats
        // "EVs: Suggest" generates a suggested EV spread like 252/252/4
        private static readonly string[] EvStats = { "HP", "ATK", "DEF", "SPA", "SPD", "SPE" };
        private static string ProcessEVs(string val)
        {
            if (val.Equals("Random", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateRandomEVs();
            }
            else if (val.Equals("Suggest", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateSuggestedEVs();
            }
            else
            {
                return $".EVs={val}";
            }
        }
        private static string GenerateRandomEVs()
        {
            int maxTotal = 510;
            int maxPerStat = 252;
            int[] evs = new int[6];

            int remaining = maxTotal;

            for (int i = 0; i < 6; i++)
            {
                int maxForStat = Math.Min(maxPerStat, remaining);
                evs[i] = Rng.Next(0, maxForStat + 1);
                remaining -= evs[i];
            }

            while (remaining > 0)
            {
                int idx = Rng.Next(0, 6);
                if (evs[idx] < maxPerStat)
                {
                    evs[idx]++;
                    remaining--;
                }
            }

            return FormatEVs(evs);
        }
        private static string GenerateSuggestedEVs()
        {
            int[] evs = new int[6];

            var indices = Enumerable.Range(0, 6).OrderBy(_ => Rng.Next()).Take(3).ToArray();

            evs[indices[0]] = 252;
            evs[indices[1]] = 252;
            evs[indices[2]] = 4;

            return FormatEVs(evs);
        }

        // Creates an ".IVs=" batch command that can be written as "IVs:" that accepts "Random" or "1IV", "2IV", "3IV", "4IV", "5IV", "6IV"
        // "IVs: Random" randomizes IVs across all stats
        // "IVs: 1IV" sets one random stat to 31 IVs, the rest are random
        // "IVs: 6IV" sets all stats to 31 IVs
        private static readonly string[] IvStats = { "HP", "ATK", "DEF", "SPA", "SPD", "SPE" };
        private static string ProcessIVs(string val)
        {
            val = val.Trim();

            if (val.Equals("Random", StringComparison.OrdinalIgnoreCase))
                return GenerateRandomIVs();

            var presetMatch = Regex.Match(val, @"^(\d)IV$", RegexOptions.IgnoreCase);
            if (presetMatch.Success)
            {
                int ivCount = int.Parse(presetMatch.Groups[1].Value);
                if (ivCount >= 1 && ivCount <= 6)
                    return GeneratePresetIVs(ivCount);
            }

            return $".IVs={val}";
        }
        private static string GenerateRandomIVs()
        {
            int maxPerStat = 31;
            int[] ivs = new int[6];
            for (int i = 0; i < 6; i++)
                ivs[i] = Rng.Next(0, maxPerStat + 1);
            return FormatIVs(ivs);
        }
        private static string GeneratePresetIVs(int countAt31)
        {
            int maxPerStat = 31;
            int[] ivs = new int[6];

            var indicesAt31 = Enumerable.Range(0, 6).OrderBy(_ => Rng.Next()).Take(countAt31).ToArray();

            foreach (var idx in indicesAt31)
                ivs[idx] = maxPerStat;

            for (int i = 0; i < 6; i++)
            {
                if (!indicesAt31.Contains(i))
                    ivs[i] = Rng.Next(0, maxPerStat + 1);
            }

            return FormatIVs(ivs);
        }

        // .GV_[STAT]= → GVs:
        // GVs now follow the same format as EVs and IVs, like below
        // GVs: 7 HP / 7 Atk / 7 Def / 7 SpA / 7 SpD / 7 Spe
        private static string ProcessGVs(string val)
        {
            var statMatches = Regex.Matches(val, @"(\d+)\s*(HP|Atk|Def|SpA|SpD|Spe)", RegexOptions.IgnoreCase);
            return string.Join("\n", statMatches.Select(stat =>
            {
                var statVal = stat.Groups[1].Value;
                var statKey = stat.Groups[2].Value.ToUpper();
                return statKey switch
                {
                    "HP" => $".GV_HP={statVal}",
                    "ATK" => $".GV_ATK={statVal}",
                    "DEF" => $".GV_DEF={statVal}",
                    "SPA" => $".GV_SPA={statVal}",
                    "SPD" => $".GV_SPD={statVal}",
                    "SPE" => $".GV_SPE={statVal}",
                    _ => string.Empty
                };
            }));
        }

        //////////////////////////////////// HELPERS //////////////////////////////////////

        private static bool TrySplitCommand(string line, out string key, out string value)
        {
            key = value = string.Empty;
            var match = Regex.Match(line.Trim(), @"^([\w\s]+)\s*:\s*(.+)$");
            if (!match.Success) return false;

            key = match.Groups[1].Value.Trim();
            value = match.Groups[2].Value.Trim();
            return true;
        }

        private static bool TryParseFlexibleDate(string input, out string formatted)
        {
            foreach (var format in AcceptedDateFormats)
            {
                if (DateTime.TryParseExact(input, format, null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    formatted = date.ToString("yyyyMMdd");
                    return true;
                }
            }
            formatted = string.Empty;
            return false;
        }

        private static bool TryParseBoolean(string input, out int result)
        {
            if (input.Equals("true", StringComparison.OrdinalIgnoreCase)) { result = 1; return true; }
            if (input.Equals("false", StringComparison.OrdinalIgnoreCase)) { result = 0; return true; }
            result = -1;
            return false;
        }

        // Alcremie forms can now add a topping to the flavor without batch command
        // For example, it accepts: Alcremie-Caramel-Swirl-Ribbon
        // Just affix the topping name to the end of Alcremie's name after its flavor
        // This code injects FormArgument/Topping for Alcremie based on Showdown Format nickname
        private static string HandleAlcremieToppings(string content)
        {
            if (!content.Contains("Alcremie", StringComparison.OrdinalIgnoreCase))
                return content;

            var match = Regex.Match(content, @"Alcremie[-\s]?.*?[-]?(Strawberry|Berry|Love|Star|Clover|Flower|Ribbon)", RegexOptions.IgnoreCase);
            if (!match.Success) return content;

            var toppingMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Strawberry", 0 }, { "Berry", 1 }, { "Love", 2 }, { "Star", 3 },
                    { "Clover", 4 }, { "Flower", 5 }, { "Ribbon", 6 }
                };

            var topping = match.Groups[1].Value;
            if (toppingMap.TryGetValue(topping, out int formArg) && !content.Contains(".FormArgument=", StringComparison.OrdinalIgnoreCase))
                content += $"\n.FormArgument={formArg}";

            return content;
        }

        private static string FormatEVs(int[] evs)
        {
            var evLines = new List<string>(6);
            for (int i = 0; i < EvStats.Length; i++)
                evLines.Add($".EV_{EvStats[i]}={evs[i]}");
            return string.Join('\n', evLines);
        }

        private static string FormatIVs(int[] ivs)
        {
            var ivLines = new List<string>(6);
            for (int i = 0; i < IvStats.Length; i++)
                ivLines.Add($".IV_{IvStats[i]}={ivs[i]}");
            return string.Join('\n', ivLines);
        }
    }
}
