using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TournamentSettings
    {
        private const string Tournament = nameof(Tournament);
        public override string ToString() => "Tournament Bot Settings";

        [Category(Tournament), Description("When enabled, a new tournament is created on startup. Make sure no rules exist yet!")]
        public bool CreateRulesOnStart { get; set; } = false;

        [Category(Tournament), Description("The custom tournament timer value")]
        public int CustomTimerValue { get; set; } = 20;

        [Category(Tournament), Description("The specified custom ruleset")]
        public int CustomRuleSet { get; set; } = 0;
    }
}
