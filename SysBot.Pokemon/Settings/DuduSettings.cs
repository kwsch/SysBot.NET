using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class DuduSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Seed Check Settings";

        [Category(FeatureToggle), Description("When enabled, Dudu checks will return all possible seed results instead of the first valid match.")]
        public bool ShowAllZ3Results { get; set; }
    }
}