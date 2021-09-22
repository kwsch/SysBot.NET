using System.ComponentModel;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class FossilSettings : IBotStateSettings
    {
        private const string Fossil = nameof(Fossil);
        public override string ToString() => "Fossil Bot Settings";

        [Category(Fossil), Description("Species of fossil Pokémon to hunt for.")]
        public FossilSpecies Species { get; set; } = FossilSpecies.Dracozolt;

        /// <summary>
        /// Toggle for injecting fossil pieces.
        /// </summary>
        [Category(Fossil), Description("Toggle for injecting fossil pieces.")]
        public bool InjectWhenEmpty { get; set; } = false;

        /// <summary>
        /// Toggle for continuing to revive fossils after condition has been met.
        /// </summary>
        [Category(Fossil), Description("When enabled, the FossilBot will continue to get fossils and dump the Pokémon into the fossil dump folder.")]
        public bool ContinueAfterMatch { get; set; } = false;

        [Category(Fossil), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; } = false;
    }
}