using System.ComponentModel;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class RaidSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Hosting = nameof(Hosting);
        public override string ToString() => "Raid Bot Settings";

        [Category(FeatureToggle), Description("The bot will reset the game assuming ldn_mitm sysmodule is on your system. Better stability.")]
        public bool UseLdnMitm { get; set; } = true;

        [Category(Hosting), Description("Minimum amount of seconds to wait before starting a raid. Ranges from 0 to 180 seconds.")]
        public int MinTimeToWait { get; set; } = 90;

        [Category(Hosting), Description("Extra time in milliseconds to wait after clicking \"Invite Others\" before locking into a Pokémon.")]
        public int ExtraTimeOpenRaid { get; set; } = 0;

        [Category(Hosting), Description("Extra time in milliseconds to wait before closing the game to reset the raid.")]
        public int ExtraTimeEndRaid { get; set; } = 0;

        [Category(Hosting), Description("Minimum Link Code to host the raid with. Set this to -1 to host with no code.")]
        public int MinRaidCode { get; set; } = 8180;

        [Category(Hosting), Description("Maximum Link Code to host the raid with. Set this to -1 to host with no code.")]
        public int MaxRaidCode { get; set; } = 8199;

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomRaidCode() => Util.Rand.Next(MinRaidCode, MaxRaidCode + 1);
    }
}