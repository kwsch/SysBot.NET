using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class FossilSettings
    {
        private const string Fossil = nameof(Fossil);
        private const string Counts = nameof(Counts);
        public override string ToString() => "Fossil Bot Settings";

        [Category(Fossil), Description("Species of fossil Pokémon to hunt for.")]
        public FossilSpecies Species { get; set; } = FossilSpecies.Dracozolt;

        /// <summary>
        /// Toggle for injecting fossil pieces.
        /// </summary>
        [Category(Fossil), Description("Toggle for injecting fossil pieces.")]
        public bool InjectWhenEmpty { get; set; }
    }
}
