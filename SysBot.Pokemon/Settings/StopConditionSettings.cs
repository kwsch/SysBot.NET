using PKHeX.Core;
using System;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class StopConditionSettings
    {
        private const string StopConditions = nameof(StopConditions);
        public override string ToString() => "Stop Condition Settings";

        [Category(StopConditions), Description("Stops only on Pokémon of this species. No restrictions if set to \"None\".")]
        public Species StopOnSpecies { get; set; }

        [Category(StopConditions), Description("Stops only on Pokémon with this FormID. No restrictions if left blank.")]
        public int? StopOnForm { get; set; }

        [Category(StopConditions), Description("Stop only on Pokémon of the specified nature.")]
        public Nature TargetNature { get; set; } = Nature.Random;

        [Category(StopConditions), Description("Minimum accepted IVs in the format HP/Atk/Def/SpA/SpD/Spe. Use \"x\" for unchecked IVs and \"/\" as a separator.")]
        public string TargetMinIVs { get; set; } = "";

        [Category(StopConditions), Description("Maximum accepted IVs in the format HP/Atk/Def/SpA/SpD/Spe. Use \"x\" for unchecked IVs and \"/\" as a separator.")]
        public string TargetMaxIVs { get; set; } = "";

        [Category(StopConditions), Description("Selects the shiny type to stop on.")]
        public TargetShinyType ShinyTarget { get; set; } = TargetShinyType.DisableOption;

        [Category(StopConditions), Description("Stop only on Pokémon that have a mark.")]
        public bool MarkOnly { get; set; } = false;

        [Category(StopConditions), Description("If MarkOnly is true, stop only on Pokémon with this specific mark.")]
        public Mark MarkMatch { get; set; } = Mark.Any;

        [Category(StopConditions), Description("Holds Capture button to record a 30 second clip when a matching Pokémon is found by EncounterBot or Fossilbot.")]
        public bool CaptureVideoClip { get; set; }

        [Category(StopConditions), Description("Extra time in milliseconds to wait after an encounter is matched before pressing Capture for EncounterBot or Fossilbot.")]
        public int ExtraTimeWaitCaptureVideo { get; set; } = 10000;

        [Category(StopConditions), Description("If set to TRUE, matches both ShinyTarget and TargetIVs settings. Otherwise, looks for either ShinyTarget or TargetIVs match.")]
        public bool MatchShinyAndIV { get; set; } = true;

        [Category(StopConditions), Description("If not empty, the provided string will be prepended to the result found log message to Echo alerts for whomever you specify. For Discord, use <@userIDnumber> to mention.")]
        public string MatchFoundEchoMention { get; set; } = string.Empty;

        public static bool EncounterFound<T>(T pk, int[] targetminIVs, int[] targetmaxIVs, StopConditionSettings settings) where T : PKM
        {
            // Match Nature and Species if they were specified.
            if (settings.StopOnSpecies != Species.None && settings.StopOnSpecies != (Species)pk.Species)
                return false;

            if (settings.StopOnForm.HasValue && settings.StopOnForm != pk.Form)
                return false;

            if (settings.TargetNature != Nature.Random && settings.TargetNature != (Nature)pk.Nature)
                return false;

            if (settings.MarkOnly && pk is IRibbonIndex m && (settings.MarkMatch == Mark.Any ? !HasMark(m) : !HasMark(m, settings.MarkMatch)))
                return false;

            if (settings.ShinyTarget != TargetShinyType.DisableOption)
            {
                bool shinymatch = settings.ShinyTarget switch
                {
                    TargetShinyType.AnyShiny => pk.IsShiny,
                    TargetShinyType.NonShiny => !pk.IsShiny,
                    TargetShinyType.StarOnly => pk.IsShiny && pk.ShinyXor != 0,
                    TargetShinyType.SquareOnly => pk.ShinyXor == 0,
                    TargetShinyType.DisableOption => true,
                    _ => throw new ArgumentException(nameof(TargetShinyType)),
                };

                // If we only needed to match one of the criteria and it shinymatch'd, return true.
                // If we needed to match both criteria and it didn't shinymatch, return false.
                if (!settings.MatchShinyAndIV && shinymatch)
                    return true;
                if (settings.MatchShinyAndIV && !shinymatch)
                    return false;
            }

            int[] pkIVList = PKX.ReorderSpeedLast(pk.IVs);

            for (int i = 0; i < 6; i++)
            {
                if (targetminIVs[i] > pkIVList[i] || targetmaxIVs[i] < pkIVList[i])
                    return false;
            }
            return true;
        }

        public static void InitializeTargetIVs(PokeTradeHub<PK8> hub, out int[] min, out int[] max)
        {
            min = ReadTargetIVs(hub.Config.StopConditions, true);
            max = ReadTargetIVs(hub.Config.StopConditions, false);
        }

        private static int[] ReadTargetIVs(StopConditionSettings settings, bool min)
        {
            int[] targetIVs = new int[6];
            char[] split = { '/' };

            string[] splitIVs = min
                ? settings.TargetMinIVs.Split(split, StringSplitOptions.RemoveEmptyEntries)
                : settings.TargetMaxIVs.Split(split, StringSplitOptions.RemoveEmptyEntries);

            // Only accept up to 6 values.  Fill it in with default values if they don't provide 6.
            // Anything that isn't an integer will be a wild card.
            for (int i = 0; i < 6; i++)
            {
                if (i < splitIVs.Length)
                {
                    var str = splitIVs[i];
                    if (int.TryParse(str, out var val))
                    {
                        targetIVs[i] = val;
                        continue;
                    }
                }
                targetIVs[i] = min ? 0 : 31;
            }
            return targetIVs;
        }

        private static bool HasMark(IRibbonIndex pk)
        {
            for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
            {
                if (pk.GetRibbon((int)mark))
                    return true;
            }
            return false;
        }

        private static bool HasMark(IRibbonIndex pk, Mark m)
        {
            return pk.GetRibbon((int)m);
        }

        public enum Mark
        {
            Any = 0,
            Lunchtime = 53,
            SleepyTime = 54,
            Dusk = 55,
            Dawn = 56,
            Cloudy = 57,
            Rainy = 58,
            Stormy = 59,
            Snowy = 60,
            Blizzard = 61,
            Dry = 62,
            Sandstorm = 63,
            Misty = 64,
            //Destiny = 65,     // Not obtainable
            //Fishing = 66,     // Fishing spawns only (encounterbot doesn't handle these)
            //Curry = 67,       // Curry spawns only (encounterbot doesn't handle these)
            Uncommon = 68,
            Rare = 69,
            Rowdy = 70,
            AbsentMinded = 71,
            Jittery = 72,
            Excited = 73,
            Charismatic = 74,
            Calmness = 75,
            Intense = 76,
            ZonedOut = 77,
            Joyful = 78,
            Angry = 79,
            Smiley = 80,
            Teary = 81,
            Upbeat = 82,
            Peeved = 83,
            Intellectual = 84,
            Ferocious = 85,
            Crafty = 86,
            Scowling = 87,
            Kindly = 88,
            Flustered = 89,
            PumpedUp = 90,
            ZeroEnergy = 91,
            Prideful = 92,
            Unsure = 93,
            Humble = 94,
            Thorny = 95,
            Vigor = 96,
            Slump = 97
        }

        public string GetPrintName(PKM pk)
        {
            var set = ShowdownParsing.GetShowdownText(pk);
            if (pk is IRibbonIndex r)
                set += GetMarkName(r);
            return set;
        }

        public static string GetMarkName(IRibbonIndex pk)
        {
            for (var mark = RibbonIndex.MarkLunchtime; mark <= RibbonIndex.MarkSlump; mark++)
            {
                if (pk.GetRibbon((int)mark))
                    return $"\nPokémon found to have **{RibbonStrings.GetName($"Ribbon{mark}")}**!";
            }
            return "";
        }
    }

    public enum TargetShinyType
    {
        DisableOption,  // Doesn't care
        NonShiny,       // Match nonshiny only
        AnyShiny,       // Match any shiny regardless of type
        StarOnly,       // Match star shiny only
        SquareOnly,     // Match square shiny only
    }
}
