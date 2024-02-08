using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
using Mirai.Net.Utils.Scaffolds;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Collections.Generic;

namespace SysBot.Pokemon.QQ
{
    public class MiraiQQTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private string Username { get; }

        private string GroupId { get; }

        public MiraiQQTradeNotifier(T data, PokeTradeTrainerInfo info, int code, string username, string groupId)
        {
            Data = data;
            Info = info;
            Code = code;
            Username = username;
            GroupId = groupId;
            LogUtil.LogText($"Created trade details for {Username} - {Code}");
        }

        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            LogUtil.LogText(message);
            if (message.Contains("Found Trading Partner:"))
            {
                Regex regex = new Regex("TID: (\\d+)");
                string tid = regex.Match(message).Groups[1].ToString();
                regex = new Regex("SID: (\\d+)");
                string sid = regex.Match(message).Groups[1].ToString();
                MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().Plain($"找到你了，你的SID7:{sid},TID7:{tid}").Build());
            }
            else if (message.StartsWith("批量"))
            {
                MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().Plain(message).Build());
            }
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            var line = $"@{info.Trainer.TrainerName}: Trade canceled, {msg}";
            LogUtil.LogText(line);
            MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At($"{info.Trainer.ID}").Plain(" 取消").Build());
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = $"@{info.Trainer.TrainerName}: " + (tradedToUser != 0
                ? $"Trade finished. Enjoy your {(Species) tradedToUser}!"
                : "Trade finished!");
            LogUtil.LogText(message);
            MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At($"{info.Trainer.ID}").Plain(" 完成").Build());
        }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            var msg =
                $"@{info.Trainer.TrainerName} (ID: {info.ID}): Initializing trade{receive} with you. Please be ready.";
            msg += $" Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(msg);
            var text = $"\n派送:{ShowdownTranslator<T>.GameStringsZh.Species[Data.Species]}\n密码:{info.Code:0000 0000}\n状态:初始化";
            List<T> batchPKMs = (List<T>)info.Context.GetValueOrDefault("batch", new List<T>());
            if (batchPKMs.Count > 1)
            {
                text = $"\n批量派送{batchPKMs.Count}只宝可梦\n密码:{info.Code:0000 0000}\n状态:初始化";
            }
            MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At($"{info.Trainer.ID}").Plain(text).Build());
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", @{name}";
            var message = $"I'm waiting for you{trainer}! My IGN is {routine.InGameName}.";
            message += $" Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(message);
            var text = $"派送:{ShowdownTranslator<T>.GameStringsZh.Species[Data.Species]}\n密码:{info.Code:0000 0000}\n状态:搜索中";
            List<T> batchPKMs = (List<T>)info.Context.GetValueOrDefault("batch", new List<T>());
            if (batchPKMs.Count > 1)
            {
                text = $"批量派送{batchPKMs.Count}只宝可梦\n密码:{info.Code:0000 0000}\n状态:搜索中";
            }
            MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At($"{info.Trainer.ID}").Plain(text).Build());
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            LogUtil.LogText(msg);
            MiraiQQBot<T>.SendGroupMessage(msg);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            var msg = $"Details for {result.FileName}: " + message;
            LogUtil.LogText(msg);
            if (result.Species != 0 && info.Type == PokeTradeType.Dump)
            {
                var text =
                    $"species:{result.Species}\npid:{result.PID}\nec:{result.EncryptionConstant}\nIVs:{string.Join(",", result.IVs)}\nisShiny:{result.IsShiny}";
                MiraiQQBot<T>.SendGroupMessage(text);
            }
        }
    }
}