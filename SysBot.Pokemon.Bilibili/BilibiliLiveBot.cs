using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Bilibili
{
    public class BilibiliLiveBot<T> where T : PKM, new()
    {
        // private readonly BilibiliSettings Settings;
        private static PokeTradeHub<T> Hub = default!;
        private static System.Collections.Generic.List<Pictocodes> lgcode;

        internal static TradeQueueInfo<T> Info => Hub.Queues.Info;

        private static readonly string DefaultUserName = "b站用户";
        private static readonly string LogIdentity = "b站直播";
        private static readonly ulong DefaultUserId = 2333;

        public BilibiliLiveBot(BilibiliSettings settings, PokeTradeHub<T> hub)
        {
            Hub = hub;
            File.WriteAllText(@"msg.txt", "等待命令");
            LogUtil.LogInfo($"日志路径:{System.Environment.CurrentDirectory}\\msg.txt", LogIdentity);
            Task.Run(() =>
            {
                var currentDanmu = "";
                while (true)
                {
                    Thread.Sleep(1000);
                    var date = DateTime.Now.ToString("yyyyMMdd");
                    string last = "";
                    try
                    {
                        var path = settings.LogUrl;
                        if (settings.LogUrl.EndsWith("/") || settings.LogUrl.EndsWith("\\"))
                        {
                            path.Remove(path.Length - 1);
                        }

                        last = File.ReadLines(@$"{path}\Log-{settings.RoomId}-{date}.txt")
                            .Last();
                    }
                    catch (Exception ex)
                    {
                        LogUtil.LogSafe(ex, LogIdentity);
                    }

                    if (string.IsNullOrWhiteSpace(last) || currentDanmu == last) continue;
                    currentDanmu = last;


                    string[] split = last.Split('：');
                    if (split.Length != 2 || Info.Count != 0) continue;
                    var showdown = ShowdownTranslator<T>.Chinese2Showdown(split[1]);
                    if (showdown.Length <= 0) continue;
                    LogUtil.LogInfo($"收到命令\n{showdown}", LogIdentity);
                    var _ = CheckAndGetPkm(showdown, DefaultUserName, out var msg, out var pkm);
                    if (!_) continue;
                    var code = Info.GetRandomTradeCode(12345);
                    File.WriteAllText(@"msg.txt",
                        $"派送:{ShowdownTranslator<T>.GameStringsZh.Species[pkm.Species]}\n密码:{code:0000 0000}");
                    var __ = AddToTradeQueue(pkm, code, DefaultUserId, DefaultUserName, RequestSignificance.Favored,
                        PokeRoutineType.LinkTrade, out string message);
                }
            });
        }


        public static bool CheckAndGetPkm(string setstring, string username, out string msg, out T outPkm)
        {
            outPkm = new T();
            if (!BilibiliLiveBot<T>.Info.GetCanQueue())
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
                msg =
                    $"Skipping trade, @{username}: Please read what you are supposed to type as the command argument.";
                return false;
            }

            if (set.InvalidLines.Count != 0)
            {
                msg =
                    $"Skipping trade, @{username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                return false;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);

                if (!pkm.CanBeTraded())
                {
                    msg = $"Skipping trade, @{username}: Provided Pokemon content is blocked from trading!";
                    return false;
                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;
                    if (valid)
                    {
                        outPkm = pk;

                        msg =
                            $"@{username} - added to the waiting list. Your request from the waiting list will be removed if you are too slow!";
                        return true;
                    }
                }

                var reason = result == "Timeout"
                    ? "Set took too long to generate."
                    : "Unable to legalize the Pokemon.";
                msg = $"Skipping trade, @{username}: {reason}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogSafe(ex, nameof(BilibiliLiveBot<T>));
                msg = $"Skipping trade, @{username}: An unexpected problem occurred.";
            }

            return false;
        }

        private static bool AddToTradeQueue(T pk, int code, ulong userId, string name, RequestSignificance sig,
            PokeRoutineType type, out string msg)
        {
            var trainer = new PokeTradeTrainerInfo(name, userId);
            var notifier = new BilibiliTradeNotifier<T>(pk, trainer, code, name);
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
            var detail =
                new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, sig == RequestSignificance.Favored);
            var uniqueTradeID = GenerateUniqueTradeID();
            var trade = new TradeEntry<T>(detail, userId, type, name, uniqueTradeID);

            var added = Info.AddToTradeQueue(trade, userId, sig == RequestSignificance.Owner);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"@{name}: Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userId, uniqueTradeID, type);
            //msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";
            msg = $" 你在第{position.Position}位";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                //msg += $". Estimated: {eta:F1} minutes.";
                msg += $", 需等待约{eta:F1}分钟";
            }

            return true;
        }
        private static int GenerateUniqueTradeID()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int randomValue = new Random().Next(1000);
            int uniqueTradeID = (int)(timestamp % int.MaxValue) * 1000 + randomValue;
            return uniqueTradeID;
        }
    }
}
