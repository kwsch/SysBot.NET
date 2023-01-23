using PKHeX.Core;
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

        [Category(Generate), Description("If PrioritizeGame is set to \"true\", uses PrioritizeGameVersion to start looking for encounters. If \"false\", uses newest game as the version.")]
        public bool PrioritizeGame { get; set; } = true;

        [Category(Generate), Description("Specifies the first game to try and generate encounters before checking other games, or current game if this field is set to \"Any\". Set PrioritizeGame to \"true\" to enable.")]
        public GameVersion PrioritizeGameVersion { get; set; } = GameVersion.Any;

        [Category(Generate), Description("Set all possible legal ribbons for any generated Pokémon.")]
        public bool SetAllLegalRibbons { get; set; }

        [Category(Generate), Description("Set a matching ball (based on color) for any generated Pokémon.")]
        public bool SetMatchingBalls { get; set; }

        [Category(Generate), Description("Force the specified ball if legal.")]
        public bool ForceSpecifiedBall { get; set; }

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

        [Category(Misc), Description("Zero out HOME tracker regardless of current tracker value. Applies to user requested PKM files as well.")]
        public bool ResetHOMETracker { get; set; } = true;
    }
}
