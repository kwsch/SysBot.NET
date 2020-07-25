using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class EggSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Egg Bot Settings";

        [Category(FeatureToggle), Description("When enabled, the EggBot will continue to get eggs and dump the Pokémon into the egg dump folder")]
        public bool ContinueAfterMatch { get; set; } = false;
        
        /*Creating a new toggle to override the default behavior if the user needs to search for non-shiny eggs*/
        
        [Category(FeatureToggle), Description("When enabled, the Eggbot will not search for Shiny generated eggs. Use for searching desired natures or IVs")]
        public bool NoShinyEggs { get; set; } = false;
        
        /*For Pokemon breeding with optimized conditions, a user might benefit from having select IVs and Nature, which can be easily controlled by the user of the
        Everstone and Destiny Knot Items. These settings are borrowed from EncounterBot settings*/
        
        [Category(Encounter), Description("Stop only on Pokémon of the specified nature.")]
        public Nature DesiredNature { get; set; } = Nature.Random;

        [Category(Encounter), Description("Targets the specified IVs HP/Atk/Def/SpA/SpD/Spe. Matches 0's and 31's, checks min value otherwise. Use \"x\" for unchecked IVs and \"/\" as a separator.")]
        public string DesiredIVs { get; set; } = "";
    }
}
