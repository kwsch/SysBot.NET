using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchCommandsHelper
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, out string msg)
        {
            ShowdownSet set = TwitchShowdownUtil.ConvertToShowdown(setstring);
            var sav = AutoLegalityWrapper.GetTrainerInfo(PKX.Generation);
            PKM pkm = sav.GetLegal(set, out _);
            var valid = new LegalityAnalysis(pkm).Valid;
            if (valid && pkm is PK8 p8)
            {
                var tq = new TwitchQueue(p8, new PokeTradeTrainerInfo(display),
                    username);
                TwitchBot.QueuePool.Add(tq);
                msg = "Added you to the waiting list. Please whisper to me your trade code! Your request from the waiting list will be removed if you are too slow!";
                return true;
            }

            msg = "Unable to legalize the Pokémon. Skipping Trade.";
            return false;
        }

        public static string ClearTrade(bool sudo, ulong userid)
        {
            var allowed = sudo || TwitchBot.Info.CanQueue;
            if (!allowed)
                return "Sorry, you are not permitted to use this command!";

            var userID = userid;
            var result = TwitchBot.Info.ClearTrade(userID);
            switch (result)
            {
                case QueueResultRemove.CurrentlyProcessing: return "Looks like you're currently being processed! Unable to remove from queue.";
                case QueueResultRemove.Removed: return "Removed you from the queue.";
                default: return "Sorry, you are not currently in the queue.";
            }
        }
    }
}
