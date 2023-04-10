using PKHeX.Core;
using SysBot.Base;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SysBot.Pokemon
{
    public class RaidSettings : IBotStateSettings, ICountSettings
    {
        private const string Hosting = nameof(Hosting);
        private const string Counts = nameof(Counts);
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Raid Bot Settings";

        [Category(Hosting), Description("Number of seconds to wait before trying to start a raid. Ranges from 0 to 180 seconds.")]
        public int TimeToWait { get; set; } = 90;

        [Category(Hosting), Description("Minimum Link Code to host the raid with. Set this to -1 to host with no code.")]
        public int MinRaidCode { get; set; } = 8180;

        [Category(Hosting), Description("Maximum Link Code to host the raid with. Set this to -1 to host with no code.")]
        public int MaxRaidCode { get; set; } = 8199;

        [Category(FeatureToggle), Description("Optional description of the raid the bot is hosting. Uses automatic Pokémon detection if left blank.")]
        public string RaidDescription { get; set; } = string.Empty;

        [Category(FeatureToggle), Description("Echoes each party member as they lock into a Pokémon.")]
        public bool EchoPartyReady { get; set; }

        [Category(FeatureToggle), Description("Allows the bot to echo your Friend Code if set.")]
        public string FriendCode { get; set; } = string.Empty;

        [Category(Hosting), Description("Number of friend requests to accept each time.")]
        public int NumberFriendsToAdd { get; set; }

        [Category(Hosting), Description("Number of friends to delete each time.")]
        public int NumberFriendsToDelete { get; set; }

        [Category(Hosting), Description("Number of raids to host before trying to add/remove friends. Setting a value of 1 will tell the bot to host one raid, then start adding/removing friends.")]
        public int InitialRaidsToHost { get; set; }

        [Category(Hosting), Description("Number of raids to host between trying to add friends.")]
        public int RaidsBetweenAddFriends { get; set; }

        [Category(Hosting), Description("Number of raids to host between trying to delete friends.")]
        public int RaidsBetweenDeleteFriends { get; set; }

        [Category(Hosting), Description("Number of row to start trying to add friends.")]
        public int RowStartAddingFriends { get; set; } = 1;

        [Category(Hosting), Description("Number of row to start trying to delete friends.")]
        public int RowStartDeletingFriends { get; set; } = 1;

        [Category(Hosting), Description("The Nintendo Switch profile you are using to manage friends. For example, set this to 2 if you are using the second profile.")]
        public int ProfileNumber { get; set; } = 1;

        [Category(FeatureToggle), Description("When enabled, the screen will be turned off during normal bot loop operation to save power.")]
        public bool ScreenOff { get; set; }

        /// <summary>
        /// Gets a random trade code based on the range settings.
        /// </summary>
        public int GetRandomRaidCode() => Util.Rand.Next(MinRaidCode, MaxRaidCode + 1);

        private int _completedRaids;

        [Category(Counts), Description("Raids Started")]
        public int CompletedRaids
        {
            get => _completedRaids;
            set => _completedRaids = value;
        }

        [Category(Counts), Description("When enabled, the counts will be emitted when a status check is requested.")]
        public bool EmitCountsOnStatusCheck { get; set; }

        public int AddCompletedRaids() => Interlocked.Increment(ref _completedRaids);

        public IEnumerable<string> GetNonZeroCounts()
        {
            if (!EmitCountsOnStatusCheck)
                yield break;
            if (CompletedRaids != 0)
                yield return $"Started Raids: {CompletedRaids}";
        }
    }
}