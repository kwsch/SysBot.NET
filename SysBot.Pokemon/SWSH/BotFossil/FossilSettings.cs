using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class FossilSettings : IBotStateSettings, ICountSettings
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

        /// <summary>
        /// Toggle for continuing to revive fossils after condition has been met.
        /// </summary>
        [Category(Fossil), Description("When enabled, the bot will continue after finding a suitable match.")]
        public ContinueAfterMatch ContinueAfterMatch { get; set; } = ContinueAfterMatch.StopExit;

        [Category(Fossil), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; }

        private int _completedFossils;

        [Category(Counts), Description("Fossil Pokémon Revived")]
        public int CompletedFossils
        {
            get => _completedFossils;
            set => _completedFossils = value;
        }

        [Category(Counts), Description("When enabled, the counts will be emitted when a status check is requested.")]
        public bool EmitCountsOnStatusCheck { get; set; }

        public int AddCompletedFossils() => Interlocked.Increment(ref _completedFossils);

        public IEnumerable<string> GetNonZeroCounts()
        {
            if (!EmitCountsOnStatusCheck)
                yield break;
            if (CompletedFossils != 0)
                yield return $"Completed Fossils: {CompletedFossils}";
        }
    }
}