using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchCommandsHelper
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, out string msg)
        {
            ShowdownSet set = TwitchShowdownUtil.ConvertToShowdown(setstring);

            if (set.Species < 1)
            {
                msg = $"Skipping trade, {username}: Please read what you are supposed to type as the command argument.";
                return false;
            }

            if (set.InvalidLines.Count != 0)
            {
                msg = $"Skipping trade, {username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                return false;
            }

            var sav = AutoLegalityWrapper.GetTrainerInfo(PKX.Generation);
            PKM pkm = sav.GetLegal(set, out _);
            var valid = new LegalityAnalysis(pkm).Valid;
            if (valid && pkm is PK8 pk8)
            {
                var tq = new TwitchQueue(pk8, new PokeTradeTrainerInfo(display), username);
                TwitchBot.QueuePool.Add(tq);
                msg = $"{username} - added to the waiting list. Please whisper to me your trade code! Your request from the waiting list will be removed if you are too slow!";
                return true;
            }

            msg = $"Skipping trade, {username}: Unable to legalize the Pokémon.";
            return false;
        }

        public static string ClearTrade(bool sudo, ulong userid)
        {
            var allowed = sudo || TwitchBot.Info.GetCanQueue();
            if (!allowed)
                return "Sorry, you are not permitted to use this command!";

            var userID = userid;
            var result = TwitchBot.Info.ClearTrade(userID);
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Removed from queue.",
                QueueResultRemove.Removed => "Removed you from the queue.",
                _ => "Sorry, you are not currently in the queue."
            };
        }
    }
}
