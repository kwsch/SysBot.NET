namespace SysBot.Pokemon;

public class PartnerDataHolder(ulong TrainerOnlineID, string TrainerName, string TrainerTID)
{
    public readonly string TrainerName = TrainerName;

    public readonly ulong TrainerOnlineID = TrainerOnlineID;

    public readonly string TrainerTID = TrainerTID;
}
