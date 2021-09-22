using System.ComponentModel;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class EncounterSettings : IBotStateSettings
    {
        private const string Encounter = nameof(Encounter);
        public override string ToString() => "Encounter Bot Settings";

        [Category(Encounter), Description("The method by which the bot will encounter Pokémon.")]
        public EncounterMode EncounteringType { get; set; } = EncounterMode.VerticalLine;

        [Category(Encounter), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; } = false;
    }
}