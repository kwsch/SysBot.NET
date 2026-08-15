namespace SysBot.Pokemon;

public enum QueueResultAdd
{
    /// <summary> Successfully added to the queue. </summary>
    Added,

    /// <summary> Can add to the queue, but was not added yet. </summary>
    CanAdd,

    /// <summary> Did not add; was already in the queue. </summary>
    AlreadyInQueue,
}
