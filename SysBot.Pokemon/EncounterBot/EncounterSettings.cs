using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class EncounterSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Encounter Bot Settings";

        [Category(FeatureToggle), Description("When enabled, the EncounterBot will continue to attempt to search for Shiny Sinistea.")]
        public bool StopOnSinistea { get; set; } = true;
    }
}