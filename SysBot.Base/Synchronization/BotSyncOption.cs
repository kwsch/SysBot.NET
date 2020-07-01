namespace SysBot.Base
{
    public enum BotSyncOption
    {
        /// <summary>
        /// Bots will ignore any synchronization barriers and will continue without caring about the other bot states.
        /// </summary>
        NoSync,

        /// <summary>
        /// Local Synchronization managed by a Barrier; releases when all bots are at the same step.
        /// </summary>
        LocalSync,
    }
}