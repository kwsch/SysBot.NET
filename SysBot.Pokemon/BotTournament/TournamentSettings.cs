using System.ComponentModel;

namespace SysBot.Pokemon.BotTournament
{
    public class TournamentSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Tournament Bot Settings";

        [Category(FeatureToggle), Description("When enabled, the TournamentBot send the tournament infinitely")]
        public bool ContinueAfterSending { get; set; } = true;

        [Category(FeatureToggle), Description("When enabled, a new tournament is created on startup")]
        public bool CreateRulesOnStart { get; set; } = false;
    }
}
