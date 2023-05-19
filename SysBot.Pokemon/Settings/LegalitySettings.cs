using PKHeX.Core;
using System.Collections.Generic;
using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class LegalitySettings
    {
        private string DefaultTrainerName = "SysBot";
        private const string Generate = nameof(Generate);
        private const string Misc = nameof(Misc);
        public override string ToString() => "Legality Generating Settings";

        // Generate
        [Category(Generate), Description("MGDB directory path for Wonder Cards.")]
        public string MGDBPath { get; set; } = string.Empty;

        [Category(Generate), Description("Folder for PKM files with trainer data to use for regenerated PKM files.")]
        public string GeneratePathTrainerInfo { get; set; } = string.Empty;

        [Category(Generate), Description("Default Original Trainer name for PKM files that don't match any of the provided PKM files.")]
        public string GenerateOT
        {
            get => DefaultTrainerName;
            set
            {
                if (!StringsUtil.IsSpammyString(value))
                    DefaultTrainerName = value;
            }
        }

        [Category(Generate), Description("Default 16-bit Trainer ID (TID) for requests that don't match any of the provided trainer data files. This should be a 5-digit number.")]
        public ushort GenerateTID16 { get; set; } = 12345;

        [Category(Generate), Description("Default 16-bit Secret ID (SID) for requests that don't match any of the provided trainer data files. This should be a 5-digit number.")]
        public ushort GenerateSID16 { get; set; } = 54321;

        [Category(Generate), Description("Default language for PKM files that don't match any of the provided PKM files.")]
        public LanguageID GenerateLanguage { get; set; } = LanguageID.English;

        [Category(Generate), Description("If PrioritizeGame is set to \"True\", uses PrioritizeGameVersion to start looking for encounters. If \"False\", uses newest game as the version. It is recommended to leave this as \"True\".")]
        public bool PrioritizeGame { get; set; } = true;

        [Category(Generate), Description("Specifies the first game to use to generate encounters, or current game if this field is set to \"Any\". Set PrioritizeGame to \"true\" to enable. It is recommended to leave this as \"Any\".")]
        public GameVersion PrioritizeGameVersion { get; set; } = GameVersion.Any;

        [Category(Generate), Description("Set all possible legal ribbons for any generated Pokémon.")]
        public bool SetAllLegalRibbons { get; set; }

        [Category(Generate), Description("Set a matching ball (based on color) for any generated Pokémon.")]
        public bool SetMatchingBalls { get; set; }

        [Category(Generate), Description("Force the specified ball if legal.")]
        public bool ForceSpecifiedBall { get; set; }

        [Category(Generate), Description("The order in which Pokémon encounter types are attempted.")]
        public List<EncounterTypeGroup> PrioritizeEncounters { get; set; } = new List<EncounterTypeGroup>() { EncounterTypeGroup.Egg, EncounterTypeGroup.Slot, EncounterTypeGroup.Static, EncounterTypeGroup.Mystery, EncounterTypeGroup.Trade};

        [Category(Generate), Description("Allow XOROSHIRO when generating Gen 8 Raid Pokémon.")]
        public bool UseXOROSHIRO { get; set; } = true;

        [Category(Generate), Description("Adds Battle Version for games that support it (SWSH only) for using past-gen Pokémon in online competitive play.")]
        public bool SetBattleVersion { get; set; }

        [Category(Generate), Description("Bot will create an Easter Egg Pokémon if provided an illegal set.")]
        public bool EnableEasterEggs { get; set; }

        [Category(Generate), Description("Allow users to submit custom OT, TID, SID, and OT Gender in Showdown sets.")]
        public bool AllowTrainerDataOverride { get; set; }

        [Category(Generate), Description("Allow users to submit further customization with Batch Editor commands.")]
        public bool AllowBatchCommands { get; set; }

        [Category(Generate), Description("Maximum time in seconds to spend when generating a set before canceling. This prevents difficult sets from freezing the bot.")]
        public int Timeout { get; set; } = 15;

        // Misc

        [Category(Misc), Description("Zero out HOME trackers for cloned and user-requested PKM files. It is recommended to leave this disabled to avoid creating invalid HOME data.")]
        public bool ResetHOMETracker { get; set; } = false;
    }
}
