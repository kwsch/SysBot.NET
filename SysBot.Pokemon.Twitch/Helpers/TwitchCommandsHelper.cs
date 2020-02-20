using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchCommandsHelper
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, out string msg)
        {
            ShowdownSet set = TwitchShowdownUtil.ConvertToShowdown(setstring);
            var sav = TrainerSettings.GetSavedTrainerData(8);
            PKM pkm = sav.GetLegalFromSet(set, out _);
            if (new LegalityAnalysis(pkm).Valid && pkm is PK8 p8)
            {
                var tq = new TwitchQueue(p8, new PokeTradeTrainerInfo(display),
                    username);
                TwitchBot.QueuePool.Add(tq);
                msg = "Added you to the waiting list. Please whisper to me your trade code! Your request from the waiting list will be removed if you are too slow!";
                return true;
            }

            msg = "Unable to legalize the pokemon. Skipping Trade.";
            return false;
        }

        public static string GetTradePosition(ulong id)
        {
            var check = TwitchBot.Info.CheckPosition(id);
            if (!check.InQueue || check.Detail is null)
                return "You are not in the queue.";

            var position = $"{check.Position}/{check.QueueCount}";
            return check.Detail.Type == PokeRoutineType.DuduBot
                ? $"You are in the Dudu queue! Position: {position}"
                : $"You are in the Trade queue! Position: {position}, Receiving: {(Species)check.Detail.Trade.TradeData.Species}";
        }

        public static string ClearTrade(bool sudo, ulong userid)
        {
            var allowed = sudo || TwitchBot.Info.CanQueue;
            if (!allowed)
                return "Sorry, you are not permitted to use this command!";

            var userID = userid;
            var result = TwitchBot.Info.ClearTrade(userID);
            switch(result)
            {
                case QueueResultRemove.CurrentlyProcessing: return "Looks like you're currently being processed! Unable to remove from queue.";
                case QueueResultRemove.Removed: return "Removed you from the queue.";
                default: return "Sorry, you are not currently in the queue.";
            };
        }

    }
}
