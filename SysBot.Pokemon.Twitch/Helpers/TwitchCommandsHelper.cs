using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchCommandsHelper
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, out string msg)
        {
            if (!TwitchBot.Info.GetCanQueue())
            {
                msg = "Sorry, I am not currently accepting queue requests!";
                return false;
            }

            var set = TwitchShowdownUtil.ConvertToShowdown(setstring);
            if (set == null)
            {
                msg = $"Skipping trade, @{username}: Empty nickname provided for the species.";
                return false;
            }
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (template.Species < 1)
            {
                msg = $"Skipping trade, @{username}: Please read what you are supposed to type as the command argument.";
                return false;
            }

            if (set.InvalidLines.Count != 0)
            {
                msg = $"Skipping trade, @{username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                return false;
            }

            var sav = AutoLegalityWrapper.GetTrainerInfo(PKX.Generation);
            PKM pkm = sav.GetLegal(template, out _);

            if (!pkm.CanBeTraded())
            {
                msg = $"Skipping trade, @{username}: Provided Pokémon content is blocked from trading!";
                return false;
            }

            var valid = new LegalityAnalysis(pkm).Valid;
            if (valid && pkm is PK8 pk8)
            {
                var tq = new TwitchQueue(pk8, new PokeTradeTrainerInfo(display), username);
                TwitchBot.QueuePool.Add(tq);
                msg = $"@{username} - added to the waiting list. Please whisper to me your trade code! Your request from the waiting list will be removed if you are too slow!";
                return true;
            }

            msg = $"Skipping trade, @{username}: Unable to legalize the Pokémon.";
            return false;
        }

        public static string ClearTrade(string user)
        {
            var result = TwitchBot.Info.ClearTrade(user);
            return GetClearTradeMessage(result);
        }

        public static string ClearTrade(ulong userID)
        {
            var result = TwitchBot.Info.ClearTrade(userID);
            return GetClearTradeMessage(result);
        }

        private static string GetClearTradeMessage(QueueResultRemove result)
        {
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Removed from queue.",
                QueueResultRemove.Removed => "Removed you from the queue.",
                _ => "Sorry, you are not currently in the queue."
            };
        }

        public static string GetCode(ulong parse)
        {
            var detail = TwitchBot.Info.GetDetail(parse);
            return detail == null
                ? "Sorry, you are not currently in the queue."
                : $"Your trade code is {detail.Trade.Code:0000}";
        }
    }
}
