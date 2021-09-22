using System.ComponentModel;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class EggSettings : IBotStateSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Egg Bot Settings";

        [Category(FeatureToggle), Description("When enabled, the EggBot will continue to get eggs and dump the Pokémon into the egg dump folder")]
        public bool ContinueAfterMatch { get; set; } = false;

        [Category(FeatureToggle), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; } = false;
    }
}