using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchCommandsHelper<T> where T : PKM, new()
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, ulong mUserId, bool sub, out string msg)
        {
            if (!TwitchBot<T>.Info.GetCanQueue())
            {
                msg = "Sorry, I am not currently accepting queue requests!";
                return false;
            }

            var set = ShowdownUtil.ConvertToShowdown(setstring);
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

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo(PKX.Generation);
                PKM pkm = sav.GetLegal(template, out var result);

                if (!pkm.CanBeTraded())
                {
                    msg = $"Skipping trade, @{username}: Provided Pok�mon content is blocked from trading!";
                    return false;
                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;
                    if (valid)
                    {
                        var tq = new TwitchQueue<T>(pk, new PokeTradeTrainerInfo(display, mUserId), username, sub);
                        TwitchBot<T>.QueuePool.RemoveAll(z => z.UserName == username); // remove old requests if any
                        TwitchBot<T>.QueuePool.Add(tq);
                        msg = $"@{username} - added to the waiting list. Please whisper your trade code to me! Your request from the waiting list will be removed if you are too slow!";
                        return true;
                    }
                }

                var reason = result == "Timeout" ? "Set took too long to generate." : "Unable to legalize the Pok�mon.";
                msg = $"Skipping trade, @{username}: {reason}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                msg = $"Skipping trade, @{username}: An unexpected problem occurred.";
            }
            return false;
        }

        public static string ClearTrade(string user)
        {
            var result = TwitchBot<T>.Info.ClearTrade(user);
            return GetClearTradeMessage(result);
        }

        public static string ClearTrade(ulong userID)
        {
            var result = TwitchBot<T>.Info.ClearTrade(userID);
            return GetClearTradeMessage(result);
        }

        private static string GetClearTradeMessage(QueueResultRemove result)
        {
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Removed from queue.",
                QueueResultRemove.Removed => "Removed you from the queue.",
                _ => "Sorry, you are not currently in the queue.",
            };
        }

        public static string GetCode(ulong parse)
        {
            var detail = TwitchBot<T>.Info.GetDetail(parse);
            return detail == null
                ? "Sorry, you are not currently in the queue."
                : $"Your trade code is {detail.Trade.Code:0000 0000}";
        }
    }
}
