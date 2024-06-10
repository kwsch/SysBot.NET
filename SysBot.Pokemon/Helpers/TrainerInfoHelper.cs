using SysBot.Base;

namespace SysBot.Pokemon.Helpers
{
    public static class TrainerInfoHelper
    {
        private const string DefaultTrainerName = "MergeBot";
        private const uint DefaultTID = 12345u;
        private const uint DefaultSID = 54321u;

        public static string AddTrainerDetails(string content, ulong userID, bool ignoreAutoOT)
        {
            if (ignoreAutoOT)
            {
                return content;
            }

            var tradeCodeStorage = new TradeCodeStorage();
            var trainerDetails = tradeCodeStorage.GetTradeDetails(userID);

            var trainerName = DefaultTrainerName;
            uint tid = DefaultTID;
            uint sid = DefaultSID;

            if (trainerDetails != null && !string.IsNullOrEmpty(trainerDetails.OT) && trainerDetails.TID != 0 && trainerDetails.SID != 0)
            {
                trainerName = trainerDetails.OT;
                tid = (uint)trainerDetails.TID;
                sid = (uint)trainerDetails.SID;
                LogUtil.LogInfo("AutoOT", $"Using trainer details from TradeCodeStorage: OT: {trainerName}, TID: {tid}, SID: {sid}");
            }

            int newlineIndex = content.IndexOf('\n');
            if (newlineIndex != -1)
            {
                content = content.Insert(newlineIndex + 1, $"OT: {trainerName}\nTID: {tid}\nSID: {sid}\n");
            }
            else
            {
                content += $"\nOT: {trainerName}\nTID: {tid}\nSID: {sid}";
            }

            return content;
        }

        public static string ModifyShowdownSetTrainerInfo(string showdownSet, ulong userID)
        {
            var tradeCodeStorage = new TradeCodeStorage();
            var trainerDetails = tradeCodeStorage.GetTradeDetails(userID);
            var trainerName = DefaultTrainerName;
            uint tid = DefaultTID;
            uint sid = DefaultSID;
            if (trainerDetails != null && !string.IsNullOrEmpty(trainerDetails.OT) && trainerDetails.TID != 0 && trainerDetails.SID != 0)
            {
                trainerName = trainerDetails.OT;
                tid = (uint)trainerDetails.TID;
                sid = (uint)trainerDetails.SID;
                LogUtil.LogInfo("AutoOT", $"Using trainer details from TradeCodeStorage: OT: {trainerName}, TID: {tid}, SID: {sid}");
            }

            var lines = showdownSet.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("OT: "))
                    lines[i] = $"OT: {trainerName}";
                else if (lines[i].StartsWith("TID: "))
                    lines[i] = $"TID: {tid}";
                else if (lines[i].StartsWith("SID: "))
                    lines[i] = $"SID: {sid}";
            }
            return string.Join("\n", lines);
        }
    }
}
