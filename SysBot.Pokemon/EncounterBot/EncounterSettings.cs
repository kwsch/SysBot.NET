using System.ComponentModel;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class EncounterSettings
    {
        private const string Encounter = nameof(Encounter);
        public override string ToString() => "Encounter Bot Settings";

        [Category(Encounter), Description("For selecting the method in which the bot will encounter Pokémon in")]
        public EncounterMode EncounteringType { get; set; } = EncounterMode.VerticalLine;

        [Category(Encounter), Description("When set to something other than None, the EncounterBot will stop upon encountering a shiny Pokémon of this species.")]
        public Species StopOnSpecies { get; set; }

    }
}