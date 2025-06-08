using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using PKHeX.Core;
using Discord;

namespace SysBot.Pokemon.Discord.Helpers;

/// <summary>
/// The Batch Commands to be converted into standard Showdown format. ///
/// </summary>
public static class BatchNormalizer
{
    private static readonly Dictionary<string, string> BatchCommandAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // New Showdown format inputs to override Batch commands that start with a period(.) //
        { "Size", "Scale" },
        { "Weight", "WeightScalar" },
        { "Height", "HeightScalar" },
        { "Met Date", "MetDate" },
        { "Met Location", "MetLocation" },
        { "Game", "Version" },
        { "Hypertrain", "HyperTrainFlags" },
        { "Moves", "Moves" },
        { "Egg Date", "EggMetDate" },
        { "Met Level", "MetLevel" },
        { "Ribbons", "Ribbons" },
        { "Mark", "Mark" },
        { "Ribbon", "Ribbon" },
        { "GVs", "GVs" },
        { "Friendship", "OriginalTrainerFriendship" },

        // Backwards compatibility to fall back on, if needed. //
        { "Scale", "Scale" },
        { "WeightScalar", "WeightScalar" },
        { "HeightScalar", "HeightScalar" },
        { "MetDate", "MetDate" },
        { "MetLocation", "MetLocation" },
        { "Version", "Version" },
        { "HyperTrainFlags", "HyperTrainFlags" },
        { "EggMetDate", "EggMetDate" },
        { "MetLevel", "MetLevel" },
    };

    // New Showdown format inputs to override Batch commands that start with an equals(=) //
    private static readonly HashSet<string> EqualCommandKeys = new(StringComparer.OrdinalIgnoreCase)
{
        "Generation",
        "Gen",
        "WasEgg",
        "Hatched"
};


    //////////////////////////////////// ACCEPTED FORMAT VALUES //////////////////////////////////////


    /// <summary>
    /// A dictionary for mapping size keywords to their corresponding ranges. ///
    /// </summary>
    private static readonly Dictionary<string, (int Min, int Max)> SizeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "XXXS", (0, 0) },
        { "XXS", (1, 30) },
        { "XS", (31, 60) },
        { "S", (61, 100) },
        { "AV", (101, 160) },
        { "L", (161, 195) },
        { "XL", (196, 241) },
        { "XXL", (242, 254) },
        { "XXXL", (255, 255) }
    };

    /// <summary>
    /// A dictionary for mapping Height & Weight scalar keywords to their corresponding ranges. ///
    /// </summary>
    private static readonly Dictionary<string, (int Min, int Max)> ScalarKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "XS", (0, 15) },
        { "S", (16, 47) },
        { "AV", (48, 207) },
        { "L", (208, 239) },
        { "XL", (240, 255) }
    };

    /// <summary>
    /// Accepted date formats for parsing flexible date inputs. ///
    /// </summary>
    private static readonly string[] AcceptedDateFormats = new[]
{
    "yyyyMMdd",
    "MMddyyyy",
    "yyyy/MM/dd",
    "MM/dd/yyyy",
    "yyyy-MM-dd",
    "MM-dd-yyyy"
};

    private static readonly Dictionary<string, int> GameKeywords = new(StringComparer.OrdinalIgnoreCase)
{
    /// <summary>
    /// Accepted Game Keywords and their corresponding Showdown version numbers. ///
    /// </summary>
    { "Red", 35 },
    { "Blue", 36 },
    { "Green", 36 }, // JP Blue = Green in INT
    { "BlueJP", 37 },
    { "Yellow", 38 },
    { "Gold", 39 },
    { "Silver", 40 },
    { "Crystal", 41 },
    { "Sapphire", 1 },
    { "Ruby", 2 },
    { "Emerald", 3 },
    { "Fire Red", 4 }, { "FR", 4 },
    { "Leaf Green", 5 }, { "LG", 5 },
    { "Colosseum", 15 }, { "XD", 15 },
    { "Heart Gold", 7 }, { "HG", 7 },
    { "Soul Silver", 8 }, { "SS", 8 },
    { "Diamond", 10 }, { "D", 10 },
    { "Pearl", 11 }, { "P", 11 },
    { "Platinum", 12 }, { "Pt", 12 },
    { "Black", 21 }, { "B", 21 },
    { "Black 2", 23 }, { "B2", 23 },
    { "White", 20 }, { "W", 20 },
    { "White 2", 22 }, { "W2", 22 },
    { "X", 24 },
    { "Y", 25 },
    { "Alpha Sapphire", 26 }, { "AS", 26 },
    { "Omega Ruby", 27 }, { "OR", 27 },
    { "Sun", 30 }, { "S", 30 },
    { "Moon", 31 }, { "M", 31 },
    { "Ultra Sun", 32 }, { "US", 32 },
    { "Ultra Moon", 33 }, { "UM", 33 },
    { "Pikachu", 42 }, { "LetsGoPikachu", 42 }, { "LGP", 42 },
    { "Eevee", 43 }, { "LetsGoEevee", 43 }, { "LGE", 43 },
    { "Pokemon GO", 34 }, { "GO", 34 },
    { "Sword", 44 }, { "SW", 44 },
    { "Shield", 45 }, { "SH", 45 },
    { "Legends Arceus", 47 }, { "PLA", 47 },
    { "Brilliant Diamond", 48 }, { "BD", 48 },
    { "Shining Pearl", 49 }, { "SP", 49 },
    { "Scarlet", 50 }, { "SL", 50 },
    { "Violet", 51 }, { "VL", 51 }
};



    //////////////////////////////////// NEW COMMAND LOGIC //////////////////////////////////////


    /// <summary>
    /// Normalizes batch commands from a given content string into a standard Showdown format. This is the logic. ///
    /// </summary>
    public static string NormalizeBatchCommands(string content)
    {
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            foreach (var (alias, key) in BatchCommandAliasMap)
            {
                var pattern = $@"^{alias}\s*:\s*(.+)$";
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                string value = match.Groups[1].Value.Trim();

                // .Scale= → Size: or Scale: // Value is a size keyword or number //
                if (key == "Scale" && SizeKeywords.TryGetValue(value, out var sizeRange))
                {
                    int random = new Random().Next(sizeRange.Min, sizeRange.Max + 1);
                    lines[i] = $".{key}={random}";
                }

                // .WeightScalar= or HeightScalar= → Weight: or Height: // Value is a size keyword or number //
                else if ((key == "WeightScalar" || key == "HeightScalar") && ScalarKeywords.TryGetValue(value, out var scalarRange))
                {
                    int random = new Random().Next(scalarRange.Min, scalarRange.Max + 1);
                    lines[i] = $".{key}={random}";
                }

                // .MetDate= or .EggMetDate= → Met Date: or Egg Date: // Value can be in various formats //
                else if ((key == "MetDate" || key == "EggMetDate") && TryParseFlexibleDate(value, out string formatted))
                {
                    lines[i] = $".{key}={formatted}";
                }

                // .Version= → Game: or Version: // Value is game name or game abbreviation //
                else if (key == "Version" && GameKeywords.TryGetValue(value.Replace(" ", ""), out int version))
                {
                    lines[i] = $".{key}={version}";
                }

                // .HyperTrainFlags= → HyperTrain: or HyperTrainFlags: // Value is "true" or "false" //
                else if (key == "HyperTrainFlags" && TryParseBoolean(value, out int boolVal))
                {
                    lines[i] = $".{key}={boolVal}";
                }

                // .Moves= → Moves: // Value is "Random"
                else if (key == "Moves" && value.Equals("Random", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $".{key}=$suggest";
                }

                // .Ribbons= → Ribbons: // Value is "All" or "None" // 
                else if (key == "Ribbons" && value.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $".{key}=$suggestAll";
                }

                // .RibbonMark[mark]=True → Mark: // Value is a mark name like "Alpha" //
                else if (key == "Mark")
                {
                    string mark = value.Replace(" ", "");
                    lines[i] = $".RibbonMark{mark}=True";
                }

                // .Ribbon[name]= → Ribbon: // Value is a Ribbon name like "Alpha" //
                else if (key == "Ribbon")
                {
                    string ribbon = value.Replace(" ", "");
                    lines[i] = $".Ribbon{ribbon}=True";
                }

                // GVs now follow this format → GVs: 7 HP / 7 Atk / 7 Def / 7 SpA / 7 SpD / 7 Spe //
                else if (key == "GVs")
                {
                    var statMatches = Regex.Matches(value, @"(\d+)\s*(HP|Atk|Def|SpA|SpD|Spe)", RegexOptions.IgnoreCase);
                    foreach (Match stat in statMatches)
                    {
                        string statVal = stat.Groups[1].Value;
                        string statKey = stat.Groups[2].Value.ToUpper();
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        string normalizedKey = statKey switch
                        {
                            "HP" => ".GV_HP",
                            "ATK" => ".GV_ATK",
                            "DEF" => ".GV_DEF",
                            "SPA" => ".GV_SPA",
                            "SPD" => ".GV_SPD",
                            "SPE" => ".GV_SPE",
                            _ => null
                        };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                        if (normalizedKey != null)
                        {
                            lines[i] = string.Empty; // clear original GVs: line
                            lines = lines.Append($"{normalizedKey}={statVal}").ToArray();
                        }
                    }
                }
                else
                {
                    lines[i] = $".{key}={value}";
                }
                break; // stop after matching the first key (the period . prefix)
            }
        }

        // Convert "Key: Value" to "=Key=Value" if Batch starts with = prefix //
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            var match = Regex.Match(line, @"^(\w+)\s*:\s*(.+)$");
            if (!match.Success) continue;

            string key = match.Groups[1].Value;
            string val = match.Groups[2].Value.Trim();

            // =Generation= → Generation: or Gen: // Value is a generation number //
            if (key.Equals("Gen", StringComparison.OrdinalIgnoreCase))
                key = "Generation";
            else if (key.Equals("Hatched", StringComparison.OrdinalIgnoreCase))
                key = "WasEgg";

            if (EqualCommandKeys.Contains(key))
                lines[i] = $"={key}={val}";
        }
        return string.Join('\n', lines);

    }


    //////////////////////////////////// HELPER METHODS //////////////////////////////////////


    /// <summary>
    /// A helper method that attempts to parse a flexible date input into a standard format for MetDate & EggMetDate. ///
    /// </summary>
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

    /// <summary>
    /// A helper method that attempts to parse a boolean input into an integer for HyperTrainFlags.
    /// </summary>
    private static bool TryParseBoolean(string input, out int result)
    {
        if (input.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            result = 1;
            return true;
        }

        if (input.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            result = 0;
            return true;
        }

        result = -1;
        return false;
    }
}
