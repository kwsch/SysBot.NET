namespace SysBot.Pokemon;

public enum QueueResultAdd
{
    /// <summary> Successfully added to the queue. </summary>
    Added,

    /// <summary> Did not add; was already in the queue. </summary>
    AlreadyInQueue,
}
