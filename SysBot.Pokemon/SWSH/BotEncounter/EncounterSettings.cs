using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class EncounterSettings : IBotStateSettings, ICountSettings
    {
        private const string Counts = nameof(Counts);
        private const string Encounter = nameof(Encounter);
        public override string ToString() => "Encounter Bot SWSH Settings";

        [Category(Encounter), Description("The method by which the Line bot will encounter Pokémon.")]
        public EncounterMode EncounteringType { get; set; } = EncounterMode.VerticalLine;

        [Category(Encounter), Description("When enabled, the bot will continue after finding a suitable match.")]
        public ContinueAfterMatch ContinueAfterMatch { get; set; } = ContinueAfterMatch.StopExit;

        [Category(Encounter), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; }

        private int _completedWild;
        private int _completedLegend;

        [Category(Counts), Description("Encountered Wild Pokémon")]
        public int CompletedEncounters
        {
            get => _completedWild;
            set => _completedWild = value;
        }

        [Category(Counts), Description("Encountered Legendary Pokémon")]
        public int CompletedLegends
        {
            get => _completedLegend;
            set => _completedLegend = value;
        }

        [Category(Counts), Description("When enabled, the counts will be emitted when a status check is requested.")]
        public bool EmitCountsOnStatusCheck { get; set; }

        public int AddCompletedEncounters() => Interlocked.Increment(ref _completedWild);
        public int AddCompletedLegends() => Interlocked.Increment(ref _completedLegend);

        public IEnumerable<string> GetNonZeroCounts()
        {
            if (!EmitCountsOnStatusCheck)
                yield break;
            if (CompletedEncounters != 0)
                yield return $"Wild Encounters: {CompletedEncounters}";
            if (CompletedLegends != 0)
                yield return $"Legendary Encounters: {CompletedLegends}";
        }
    }
}