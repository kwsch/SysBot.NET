using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class CountSettings
    {
        private const string Trades = nameof(Trades);
        private const string Received = nameof(Received);
        public override string ToString() => "Completed Counts Storage";

        [Category(Trades), Description("Completed Surprise Trades")]
        public int CompletedSurprise { get; set; }

        [Category(Trades), Description("Completed Link Trades (Distribution)")]
        public int CompletedDistribution { get; set; }

        [Category(Trades), Description("Completed Link Trades (Specific User)")]
        public int CompletedTrades { get; set; }

        [Category(Trades), Description("Completed Seed Check Trades")]
        public int CompletedSeedChecks { get; set; }

        [Category(Trades), Description("Completed Clone Trades (Specific User)")]
        public int CompletedClones { get; set; }

        [Category(Trades), Description("Completed Dump Trades (Specific User)")]
        public int CompletedDumps { get; set; }

        // Received

        [Category(Received), Description("Eggs Retrieved")]
        public int CompletedEggs { get; set; }

        [Category(Received), Description("Fossil Pokémon Revived")]
        public int CompletedFossils { get; set; }

        [Category(Received), Description("Raids Started")]
        public int CompletedRaids { get; set; }
    }
}