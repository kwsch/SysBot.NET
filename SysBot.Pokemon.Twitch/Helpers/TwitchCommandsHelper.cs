using PKHeX.Core;
using SysBot.Base;
using System;

namespace SysBot.Pokemon.Twitch;

public static class TwitchCommandsHelper<T> where T : PKM, new()
{
    // Helper functions for commands
    public static bool AddToWaitingList(string setstring, string display, string username, ulong mUserId, bool sub, out string msg)
    {
        if (!TwitchBot<T>.Info.GetCanQueue())
        {
            msg = "Sorry, Ich akzeptiere momentan keine Anfragen!";
            return false;
        }

        var set = ShowdownUtil.ConvertToShowdown(setstring);
        if (set == null)
        {
            msg = $"Skippe Handel, @{username}: Leerer Spitzname angegeben für die Species.";
            return false;
        }
        var template = AutoLegalityWrapper.GetTemplate(set);
        if (template.Species < 1)
        {
            msg = $"Skippe Handel, @{username}: Bitte Lese wie du den Befehl benutzen sollst.";
            return false;
        }

        if (set.InvalidLines.Count != 0)
        {
            msg = $"Skippe Handel, @{username}: konnte Showdown Set nicht verarbeiten:\n{string.Join("\n", set.InvalidLines)}";
            return false;
        }

        try
        {
            var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
            PKM pkm = sav.GetLegal(template, out var result);

            if (!pkm.CanBeTraded())
            {
                msg = $"Skippe Handel, @{username}: Dieses Pokémon ust so nicht Handelbar!";
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
                    msg = $"@{username} - wurde der Warteliste hinzugefügt. Bitte flüstere mir deinen TradeCode! Deine Anfrage wird von der Warteliste wieder entfernt wenn du zu langsam bist!";
                    return true;
                }
            }

            var reason = result == "Timeout" ? "Set took too long to generate." : "Unable to legalize the Pokémon.";
            msg = $"Skipping trade, @{username}: {reason}";
        }
        catch (Exception ex)
        {
            LogUtil.LogSafe(ex, nameof(TwitchCommandsHelper<T>));
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
            QueueResultRemove.CurrentlyProcessing => "Du bist scheinbar gerade an der Reihe! Nicht von der Warteliste entfernt.",
            QueueResultRemove.CurrentlyProcessingRemoved => "Du bist scheinbar gerade an der Reihe! Du wurdest von der Warteliste entfernt.",
            QueueResultRemove.Removed => "habe dich von der Warteliste entfernt.",
            _ => "Sorry, you are not currently in the queue.",
        };
    }

    public static string GetCode(ulong parse)
    {
        var detail = TwitchBot<T>.Info.GetDetail(parse);
        return detail == null
            ? "Sorry, du bist nicht in der Warteliste."
            : $"Dein Tauschcode ist {detail.Trade.Code:0000 0000}";
    }
}
