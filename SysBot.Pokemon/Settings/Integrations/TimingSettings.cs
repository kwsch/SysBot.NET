using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TimingSettings
    {
        private const string OpenGame = nameof(OpenGame);
        private const string CloseGame = nameof(CloseGame);
        private const string Raid = nameof(Raid);
        private const string Misc = nameof(Misc);
        public override string ToString() => "Extra Time Settings";

        // Opening the game.
        [Category(OpenGame), Description("Extra time in milliseconds to wait for profiles to load when starting the game.")]
        public int ExtraTimeLoadProfile { get; set; } = 0;

        [Category(OpenGame), Description("Extra time in milliseconds to wait to check if DLC is usable.")]
        public int ExtraTimeCheckDLC { get; set; } = 0;

        [Category(OpenGame), Description("Extra time in milliseconds to wait before clicking A in title screen.")]
        public int ExtraTimeLoadGame { get; set; } = 5000;

        // Closing the game.
        [Category(CloseGame), Description("Extra time in milliseconds to wait after pressing HOME to minimize the game.")]
        public int ExtraTimeReturnHome { get; set; } = 0;

        [Category(CloseGame), Description("Extra time in milliseconds to wait after clicking to close the game.")]
        public int ExtraTimeCloseGame { get; set; } = 0;

        // Raid-specific timings.
        [Category(Raid), Description("[RaidBot] Extra time in milliseconds to wait for the raid to load after clicking on the den.")]
        public int ExtraTimeLoadRaid { get; set; } = 0;

        [Category(Raid), Description("[RaidBot] Extra time in milliseconds to wait after clicking \"Invite Others\" before locking into a Pokémon.")]
        public int ExtraTimeOpenRaid { get; set; } = 0;

        [Category(Raid), Description("[RaidBot] Extra time in milliseconds to wait before closing the game to reset the raid.")]
        public int ExtraTimeEndRaid { get; set; } = 0;

        [Category(Raid), Description("[RaidBot] Extra time in milliseconds to wait after accepting a friend.")]
        public int ExtraTimeAddFriend { get; set; } = 0;

        [Category(Raid), Description("[RaidBot] Extra time in milliseconds to wait after deleting a friend.")]
        public int ExtraTimeDeleteFriend { get; set; } = 0;

        // Miscellaneous settings.
        [Category(Misc), Description("Extra time in milliseconds to wait after clicking + to reconnect to Y-Comm.")]
        public int ExtraTimeReconnectYComm { get; set; } = 0;

        [Category(Misc), Description("Time to wait after opening the keyboard for code entry during trades.")]
        public int ExtraTimeOpenCodeEntry { get; set; } = 1000;

        [Category(Misc), Description("Time to wait after each keypress when navigating Switch menus or entering Link Code.")]
        public int KeypressTime { get; set; } = 200;

        [Category(Misc), Description("Enable this to decline incoming system updates.")]
        public bool AvoidSystemUpdate { get; set; } = false;
    }
}
