using System.ComponentModel;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class EncounterSettings
    {
        private const string Encounter = nameof(Encounter);
        public override string ToString() => "Encounter Bot Settings";

        [Category(Encounter), Description("The method by which the bot will encounter Pokémon.")]
        public EncounterMode EncounteringType { get; set; } = EncounterMode.VerticalLine;

        [Category(Encounter), Description("Stops upon encountering a shiny Pokémon of this species. Stops on any shiny if set to \"None\". Does not apply to legendary encounters.")]
        public Species StopOnSpecies { get; set; }

        [Category(Encounter), Description("Stop only on Pokémon of the specified nature.")]
        public Nature DesiredNature { get; set; } = Nature.Random;

        [Category(Encounter), Description("Targets the specified IVs HP/Atk/Def/SpA/SpD/Spe. Matches 0's and 31's, checks min value otherwise. Use \"x\" for unchecked IVs and \"/\" as a separator.")]
        public string DesiredIVs { get; set; } = "";
    }
}