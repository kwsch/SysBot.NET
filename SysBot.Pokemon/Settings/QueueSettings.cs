using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class QueueSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        public override string ToString() => "Queue Joining Settings";

        [Category(FeatureToggle), Description("Toggles if users can join the queue.")]
        public bool CanQueue { get; set; } = true;

        [Category(FeatureToggle), Description("Prevents adding users if there are this many users in the queue already.")]
        public int MaxQueueCount { get; set; } = 999;
    }
}