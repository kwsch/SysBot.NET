using PKHeX.Core;

namespace SysBot.Pokemon.Helpers
{
    public static class TrainerInfoHelper
    {
        private const string DefaultTrainerName = "MergeBot";
        private const uint DefaultTID = 12345u;
        private const uint DefaultSID = 54321u;
        private const int DefaultLanguage = (int)LanguageID.English;

        public static (string trainerName, uint tid, uint sid, int language) GetTrainerDetails(ulong userID)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            var trainerDetails = tradeCodeStorage.GetTradeDetails(userID);
            var trainerName = DefaultTrainerName;
            uint tid = DefaultTID;
            uint sid = DefaultSID;
            int language = DefaultLanguage;

            if (trainerDetails != null && !string.IsNullOrEmpty(trainerDetails.OT) && trainerDetails.TID != 0 && trainerDetails.SID != 0)
            {
                trainerName = trainerDetails.OT;
                tid = (uint)trainerDetails.TID;
                sid = (uint)trainerDetails.SID;
                language = trainerDetails.Language != -1 ? trainerDetails.Language : DefaultLanguage;
            }

            // Ensure language and gender have valid defaults if not provided
            if (language == -1) language = (int)LanguageID.English;

            return (trainerName, tid, sid, language);
        }
    }
}
