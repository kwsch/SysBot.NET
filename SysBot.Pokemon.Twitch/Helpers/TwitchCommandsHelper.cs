using System;
using System.IO;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchCommandsHelper<T> where T : PKM, new()
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, ulong mUserId, bool sub, out string msg, PokeTradeHub<T> Hub)
        {
            if (!TwitchBot<T>.Info.GetCanQueue())
            {
                msg = "Sorry, I am still booting! Try again in a minute!";
                return false;
            }

                var set = ShowdownUtil.ConvertToShowdown(setstring, Hub.Config.Twitch.CreateEggsWithNickname);
                if (set == null)
                {
                    msg = $"Skipping trade, @{username}: Empty nickname provided for the species.";
                    return false;
                }
                var template = AutoLegalityWrapper.GetTemplate(set);
                if (template.Species < 1)
                {
                    msg = $"Skipping trade, @{username}: Incorrect Trade request. Check '!tradeguide' to get more informations.";
                    return false;
                }

                if (set.InvalidLines.Count != 0)
                {
                    msg = $"Skipping trade, @{username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                    return false;
                }
            
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                PKM pkm = sav.GetLegal(template, out var result);

                //                if (!pkm.CanBeTraded())
                //                {
                //                    msg = $"Skipping trade, @{username}: Provided Pokémon content is blocked from trading!";
                //                    return false;
                //                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;

                    if (pkm.Nickname == "Egg" && Hub.Config.Twitch.CreateEggsWithNickname)
                    {
                        if(!sub && Hub.Config.Twitch.EggsSubsOnly)
                        {
                            msg = $"Skipping trade, @{username}: Only Subscriber can request Eggs";
                            return false;
                        }
                        pkm.IsEgg = true;
                        pkm.Egg_Location = 60010;
                        pkm.Met_Location = 0;
                        pkm.CurrentFriendship = 1;
                        pkm.OT_Friendship = 1;
                        pkm.Met_Level = 1;
                    }



                    if (!sub && Hub.Config.Twitch.AutoNicknameNonSubs)
                    {
                        pkm.IsNicknamed = true;
                        pkm.Nickname = Hub.Config.Twitch.AutoNickname;
                     //   pkm.HeldItem = 0; // no item for follower
                    }

                    if (!sub && Hub.Config.Twitch.AutoOTNonSubs)
                    {
                        pkm.OT_Name = Hub.Config.Twitch.AutoOT;
                    }

                    if (!sub && Hub.Config.Twitch.HeldItemsSubsOnly)
                    {
                        pkm.HeldItem = 0;
                    }

                    if (valid)
                    {
                        var tq = new TwitchQueue<T>(pk, new PokeTradeTrainerInfo(display, mUserId), username, sub);
                        TwitchBot<T>.QueuePool.RemoveAll(z => z.UserName == username); // remove old requests if any
                        TwitchBot<T>.QueuePool.Add(tq);
                        if (sub)
                        {
                            msg = $"@{username} - added {(Species)pkm.Species} to the Subscriber Priority list. Please whisper your trade code to me! Your request from the waiting list will be removed if you are too slow!";
                            return true;
                        }
                        msg = $"@{username} - added {(Species)pkm.Species} to the waiting list. Please whisper your trade code to me! Your request from the waiting list will be removed if you are too slow!";
                        return true;
                    }
                }

                var reason = result == "Timeout" ? "Set took too long to generate." : "Pokémon is not legal. Check your Request and remove illegal settings.";
                msg = $"Skipping trade, @{username}: {reason}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogSafe(ex, nameof(TwitchCommandsHelper<T>));
                msg = $"Skipping trade, @{username}: An unexpected problem occurred.";
            }
            return false;
        }

        public static bool AddToWaitingListSpecial(string setstring, string display, string username, ulong mUserId, bool sub, out string msg, PokeTradeHub<T> Hub)
        {
            if (!TwitchBot<T>.Info.GetCanQueue())
            {
                msg = "Sorry, I am still booting! Try again in a minute!";
                return false;
            }
            if (setstring == null) 
            {
                msg = $"@{username} - Trade cancelled, no Pokemon was selected. Choose one of the following Special Trades: Mew, Celebi, Jirachi, Deoxys, Deoxys-A, Deoxys-D or Deoxys-S";
                return false;
            }
            try
           {
                var file = $"{Hub.Config.Folder.DistributeFolder}" + "\\mew.pb8";
                if (setstring == "Mew")
                {
                    file = $"{Hub.Config.Folder.DistributeFolder}" + "\\mew.pb8";
                }
                else if (setstring == "Celebi")
                {
                file = $"{Hub.Config.Folder.DistributeFolder}" + "\\celebi.pb8";
                }
                else if (setstring == "Jirachi")
                {
                    file = $"{Hub.Config.Folder.DistributeFolder}" + "\\jirachi.pb8";
                }
                else if (setstring == "Deoxys")
                {
                    file = $"{Hub.Config.Folder.DistributeFolder}" + "\\deoxys.pb8";
                }
                else if (setstring == "Deoxys-A")
                {
                    file = $"{Hub.Config.Folder.DistributeFolder}" + "\\deoxys-a.pb8";
                }
                else if (setstring == "Deoxys-D")
                {
                    file = $"{Hub.Config.Folder.DistributeFolder}" + "\\deoxys-d.pb8";
                }
                else if (setstring == "Deoxys-S")
                {
                    file = $"{Hub.Config.Folder.DistributeFolder}" + "\\deoxys-s.pb8";
                }
                else
                {
                    msg = $"@{username} - Trade cancelled, wrong input. No customization possible. Choose one of the following Special Trades: Mew, Celebi, Jirachi, Deoxys, Deoxys-A, Deoxys-D or Deoxys-S";
                    return false;
                }

                var data = File.ReadAllBytes(file);
                var pkm = PKMConverter.GetPKMfromBytes(data);
                if (pkm is null)
                {
                    msg = "Unkown Error";
                    return false;
                }
                if (pkm is not T)
                {
                PKMConverter.AllowIncompatibleConversion = true;
                pkm = PKMConverter.ConvertToType(pkm, typeof(T), out _);
                }
                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;

                    if (!sub && Hub.Config.Twitch.AutoNicknameNonSubs)
                    {
                        pkm.IsNicknamed = true;
                        pkm.Nickname = Hub.Config.Twitch.AutoNickname;
                        //   pkm.HeldItem = 0; // no item for follower
                    }

                    if (!sub && Hub.Config.Twitch.AutoOTNonSubs)
                    {
                        pkm.OT_Name = Hub.Config.Twitch.AutoOT;
                    }

                    if (!sub && Hub.Config.Twitch.HeldItemsSubsOnly)
                    {
                        pkm.HeldItem = 0;
                    }

                    if (true)
                    {
                        var tq = new TwitchQueue<T>(pk, new PokeTradeTrainerInfo(display, mUserId), username, sub);
                        TwitchBot<T>.QueuePool.RemoveAll(z => z.UserName == username); // remove old requests if any
                        TwitchBot<T>.QueuePool.Add(tq);
                        if (sub)
                        {
                            msg = $"@{username} - added {(Species)pkm.Species} to the Subscriber Priority list. Please whisper your trade code to me! Your request from the waiting list will be removed if you are too slow!";
                            return true;
                        }
                        msg = $"@{username} - added {(Species)pkm.Species} to the waiting list. Please whisper your trade code to me! Your request from the waiting list will be removed if you are too slow!";
                        return true;
                    }
                }

                var reason = "Unknown Error";
                msg = $"Skipping trade, @{username}: {reason}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
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
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Did not remove from queue.",
                QueueResultRemove.CurrentlyProcessingRemoved => "Looks like you're currently being processed! Removed from queue.",
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
