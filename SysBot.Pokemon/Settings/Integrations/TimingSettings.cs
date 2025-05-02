using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TimingSettings
    {
        private const string TimingsCategory = "Timings";

        [Category(TimingsCategory), TypeConverter(typeof(ExpandableObjectConverter))]
        public MiscellaneousSettingsCategory MiscellaneousSettings { get; set; } = new();

        [Category(TimingsCategory), TypeConverter(typeof(ExpandableObjectConverter))]
        public OpeningGameSettingsCategory OpeningGameSettings { get; set; } = new();

        [Category(TimingsCategory), TypeConverter(typeof(ExpandableObjectConverter))]
        public RaidSettingsCategory RaidSettings { get; set; } = new();

        [Category(TimingsCategory), TypeConverter(typeof(ExpandableObjectConverter))]
        public ClosingGameSettingsCategory ClosingGameSettings { get; set; } = new();

        public override string ToString() => "Timing Settings";
    }

    // Miscellaneous settings category
    public class MiscellaneousSettingsCategory
    {
        public override string ToString() => "Miscellaneous Settings";

        [Description("Enable this to decline incoming system updates.")]
        public bool AvoidSystemUpdate { get; set; }

        [Description("Extra time in milliseconds to wait between attempts to reconnect. Base time is 30 seconds.")]
        public int ExtraReconnectDelay { get; set; }

        [Description("[SWSH/SV] Extra time in milliseconds to wait after clicking + to connect to Y-Comm (SWSH) or L to connect online (SV).")]
        public int ExtraTimeConnectOnline { get; set; }

        [Description("[BDSP] Extra time in milliseconds to wait for the Union Room to load before trying to call for a trade.")]
        public int ExtraTimeJoinUnionRoom { get; set; } = 500;

        [Description("[BDSP] Extra time in milliseconds to wait for the overworld to load after leaving the Union Room.")]
        public int ExtraTimeLeaveUnionRoom { get; set; } = 1000;

        [Description("[SV] Extra time in milliseconds to wait for the Poké Portal to load.")]
        public int ExtraTimeLoadPortal { get; set; } = 1000;

        [Description("Extra time in milliseconds to wait for the box to load after finding a trade.")]
        public int ExtraTimeOpenBox { get; set; } = 1000;

        [Description("Time to wait after opening the keyboard for code entry during trades.")]
        public int ExtraTimeOpenCodeEntry { get; set; } = 1000;

        [Description("[BDSP] Extra time in milliseconds to wait for the Y Menu to load at the start of each trade loop.")]
        public int ExtraTimeOpenYMenu { get; set; } = 500;

        [Description("Time to wait after each keypress when navigating Switch menus or entering Link Code.")]
        public int KeypressTime { get; set; } = 200;

        [Description("Number of times to attempt reconnecting to a socket connection after a connection is lost. Set this to -1 to try indefinitely.")]
        public int ReconnectAttempts { get; set; } = 30;
    }

    // Opening the game settings category
    public class OpeningGameSettingsCategory
    {
        public override string ToString() => "Opening the Game";

        [Description("Extra time in milliseconds to wait to check if DLC is usable.")]
        public int ExtraTimeCheckDLC { get; set; }

        [Description("Extra time in milliseconds to wait before clicking A in title screen.")]
        public int ExtraTimeLoadGame { get; set; } = 5000;

        [Description("[BDSP] Extra time in milliseconds to wait for the overworld to load after the title screen.")]
        public int ExtraTimeLoadOverworld { get; set; } = 3000;

        [Description("Enable this if you need to select a profile when starting the game.")]
        public bool ProfileSelectionRequired { get; set; } = true;

        [Description("Extra time in milliseconds to wait for profiles to load when starting the game.")]
        public int ExtraTimeLoadProfile { get; set; }

        [Description("Enable this to add a delay for \"Checking if Game Can be Played\" Pop-up.")]
        public bool CheckGameDelay { get; set; } = false;

        [Description("Extra Time to wait for the \"Checking if Game Can Be Played\" Pop-up.")]
        public int ExtraTimeCheckGame { get; set; } = 200;
    }

    // Raid-specific timings settings category
    public class RaidSettingsCategory
    {
        public override string ToString() => "Raid-specific Timings";

        [Description("[RaidBot] Extra time in milliseconds to wait after accepting a friend.")]
        public int ExtraTimeAddFriend { get; set; }

        [Description("[RaidBot] Extra time in milliseconds to wait after deleting a friend.")]
        public int ExtraTimeDeleteFriend { get; set; }

        [Description("[RaidBot] Extra time in milliseconds to wait before closing the game to reset the raid.")]
        public int ExtraTimeEndRaid { get; set; }

        [Description("[RaidBot] Extra time in milliseconds to wait for the raid to load after clicking on the den.")]
        public int ExtraTimeLoadRaid { get; set; }

        [Description("[RaidBot] Extra time in milliseconds to wait after clicking \"Invite Others\" before locking into a Pokémon.")]
        public int ExtraTimeOpenRaid { get; set; }
    }

    // Closing the game settings category
    public class ClosingGameSettingsCategory
    {
        public override string ToString() => "Closing the Game";

        [Description("Extra time in milliseconds to wait after clicking to close the game.")]
        public int ExtraTimeCloseGame { get; set; }

        [Description("Extra time in milliseconds to wait after pressing HOME to minimize the game.")]
        public int ExtraTimeReturnHome { get; set; }
    }
}
