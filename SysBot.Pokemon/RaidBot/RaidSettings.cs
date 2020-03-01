using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class RaidSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Hosting = nameof(Hosting);
        public override string ToString() => "Raid Bot Settings";

        [Category(FeatureToggle), Description("When set, the bot will assume that ldn_mitm sysmodule is running on your system. Better stability")]
        public bool UseLdnMitm { get; set; } = true;

        [Category(Hosting), Description("Link Code to host the raid with.")]
        public int RaidCode { get; set; } = 1337;
    }
}