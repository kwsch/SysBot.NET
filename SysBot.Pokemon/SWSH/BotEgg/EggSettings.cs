using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class EggSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Egg Bot Settings";

        [Category(FeatureToggle), Description("When enabled, the EggBot will continue to get eggs and dump the Pokémon into the egg dump folder")]
        public bool ContinueAfterMatch { get; set; } = false;
    }
}