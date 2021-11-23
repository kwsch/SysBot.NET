namespace SysBot.Pokemon
{
    public class PartnerDataHolder
    {
        public readonly ulong TrainerOnlineID;
        public readonly string TrainerName;
        public readonly string TrainerTID;

        public PartnerDataHolder(ulong trainerNid, string trainerName, string trainerTid)
        {
            TrainerOnlineID = trainerNid;
            TrainerName = trainerName;
            TrainerTID = trainerTid;
        }
    }
}
