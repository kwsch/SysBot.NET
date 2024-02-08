using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Modules;
using Mirai.Net.Utils.Scaffolds;
using PKHeX.Core;
using System.Linq;
using System.Reactive.Linq;

namespace SysBot.Pokemon.QQ
{
    public class CommandModule<T> : IModule where T : PKM, new()
    {
        public bool? IsEnable { get; set; } = true;

        public void Execute(MessageReceiverBase @base)
        {
            var receiver = @base.Concretize<GroupMessageReceiver>();
            QQSettings settings = MiraiQQBot<T>.Settings;

            if (receiver.MessageChain.OfType<AtMessage>().All(x => x.Target != settings.QQ)) return;

            var text = receiver.MessageChain.OfType<PlainMessage>()?.FirstOrDefault()?.Text ?? "";
            if (string.IsNullOrWhiteSpace(text)) return;
            if (text.Trim().StartsWith("取消"))
            {
                var result = MiraiQQBot<T>.Info.ClearTrade(ulong.Parse(receiver.Sender.Id));
                MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At(receiver.Sender.Id).Plain($" {GetClearTradeMessage(result)}").Build());
            }
            else if (text.Trim().StartsWith("位置"))
            {
                var result = MiraiQQBot<T>.Info.CheckPosition(ulong.Parse(receiver.Sender.Id));
                MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At(receiver.Sender.Id).Plain($" {GetQueueCheckResultMessage(result)}").Build());
            }
        }

        public static string GetQueueCheckResultMessage(QueueCheckResult<T> result)
        {
            if (!result.InQueue || result.Detail is null)
                return "你不在队列里";
            var msg = $"你在第{result.Position}位";
            var pk = result.Detail.Trade.TradeData;
            if (pk.Species != 0)
                msg += $"，交换宝可梦：{ShowdownTranslator<T>.GameStringsZh.Species[result.Detail.Trade.TradeData.Species]}";
            return msg;
        }

        private static string GetClearTradeMessage(QueueResultRemove result)
        {
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "你正在交换中",
                QueueResultRemove.CurrentlyProcessingRemoved => "正在删除",
                QueueResultRemove.Removed => "已删除",
                _ => "你不在队列里",
            };
        }
    }
}
