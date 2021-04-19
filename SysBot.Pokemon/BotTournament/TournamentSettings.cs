using System.ComponentModel;

namespace SysBot.Pokemon.BotTournament
{
    public class TournamentSettings
    {
        private const string Tournament = nameof(Tournament);
        public override string ToString() => "Tournament Bot Settings";

        [Category(Tournament), Description("When enabled, a new tournament is created on startup")]
        public bool CreateRulesOnStart { get; set; } = false;

        [Category(Tournament), Description("The custom tournament timer value")]
        public int CustomTimerValue { get; set; } = 20;

        [Category(Tournament), Description("The custom tournament name value")]
        public string CustomTournamentName { get; set; } = "Timer Tournament";
    }
}
