namespace SysBot.Pokemon;

public enum QueueResultRemove
{
    /// <summary> Successfully removed from the queue. </summary>
    Removed,

    /// <summary> Did not remove; was just removed and is being processed. </summary>
    CurrentlyProcessing,

    /// <summary> Did not remove; was just removed and is being processed. </summary>
    CurrentlyProcessingRemoved,

    /// <summary> Did not remove; was not in queue to begin with. </summary>
    NotInQueue,
}
