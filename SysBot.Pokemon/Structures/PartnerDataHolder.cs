namespace SysBot.Pokemon;

public class PartnerDataHolder(ulong TrainerOnlineID, string TrainerName, string TrainerTID)
{
    public readonly ulong TrainerOnlineID = TrainerOnlineID;
    public readonly string TrainerName = TrainerName;
    public readonly string TrainerTID = TrainerTID;

    public object Language { get; internal set; }
    public object Gender { get; internal set; }
    public object SID { get; internal set; }
    public object TID { get; internal set; }
}
