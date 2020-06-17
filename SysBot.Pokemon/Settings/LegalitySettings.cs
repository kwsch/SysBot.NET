using System.ComponentModel;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class LegalitySettings
    {
        private const string Generate = nameof(Generate);
        private const string Misc = nameof(Misc);
        public override string ToString() => "Legality Generating Settings";

        // Generate

        [Category(Generate), Description("Regenerated PKM files will attempt to be sourced from games using trainer data info from these PKM Files.")]
        public string GeneratePathTrainerInfo { get; set; } = string.Empty;

        [Category(Generate), Description("Default Trainer Name for PKM files that can't originate from any of the provided SaveFiles.")]
        public string GenerateOT { get; set; } = "SysBot";

        [Category(Generate), Description("Default 16 Bit Trainer ID (TID) for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateTID16 { get; set; } = 12345;

        [Category(Generate), Description("Default 16 Bit Secret ID (SID) for PKM files that can't originate from any of the provided SaveFiles.")]
        public int GenerateSID16 { get; set; } = 54321;

        [Category(Generate), Description("Default Language for PKM files that can't originate from any of the provided SaveFiles.")]
        public LanguageID GenerateLanguage { get; set; } = LanguageID.English;

        [Category(Generate), Description("Set all possible ribbons for any generated Pokémon.")]
        public bool SetAllLegalRibbons { get; set; }

        [Category(Generate), Description("Set a matching ball (based on color) for any generated Pokémon.")]
        public bool SetMatchingBalls { get; set; }

        [Category(Generate), Description("Force the specified ball by iterating through all encounters and finding a legal one with the specific ball")]
        public bool ForceSpecifiedBall { get; set; } = false;

        [Category(Generate), Description("Allow Brute Forcing to make something legal (CPU Intensive)")]
        public bool AllowBruteForce { get; set; }

        [Category(Generate), Description("Allow XOROSHIRO")]
        public bool UseXOROSHIRO { get; set; } = true;

        [Category(Generate), Description("Bot will create an Easter Egg Pokémon if provided an illegal set.")]
        public bool EnableEasterEggs { get; set; } = false;

        [Category(Generate), Description("When set, the bot will only send a Pokémon if it is legal!")]
        public bool VerifyLegality { get; set; } = true;

        // Misc

        [Category(Misc), Description("Zero out HOME tracker regardless of current tracker value. Applies to user requested PKM files as well.")]
        public bool ResetHOMETracker { get; set; } = true;
    }
}