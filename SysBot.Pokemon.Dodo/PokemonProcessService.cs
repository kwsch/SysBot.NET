using System;
using System.Net.Http;
using DoDo.Open.Sdk.Models.Bots;
using DoDo.Open.Sdk.Models.ChannelMessages;
using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Services;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;

namespace SysBot.Pokemon.Dodo
{
    public class PokemonProcessService<TP> : EventProcessService where TP : PKM, new()
    {
        private readonly OpenApiService _openApiService;
        private static readonly string LogIdentity = "DodoBot";
        private static readonly string Welcome = "at我并尝试对我说：\n皮卡丘\nps代码\n或者直接拖一个文件进来";
        private readonly string _channelId;
        private DodoSettings _dodoSettings;
        private string _botDodoSourceId = default!;

        public PokemonProcessService(OpenApiService openApiService, DodoSettings settings)
        {
            _openApiService = openApiService;
            _channelId = settings.ChannelId;
            _dodoSettings = settings;
        }

        public override void Connected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Disconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Reconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Exception(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void PersonalMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyPersonalMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;

            if (eventBody.MessageBody is MessageBodyText messageBodyText)
            {
                DodoBot<TP>.SendPersonalMessage(eventBody.DodoSourceId, $"你好", eventBody.IslandSourceId);
            }
        }

        public override void ChannelMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;
            if (!string.IsNullOrWhiteSpace(_channelId) && eventBody.ChannelId != _channelId) return;

            if (eventBody.MessageBody is MessageBodyFile messageBodyFile)
            {
                if (!FileTradeHelper<TP>.ValidFileSize(messageBodyFile.Size ?? 0) || !FileTradeHelper<TP>.ValidFileName(messageBodyFile.Name))
                {
                    ProcessWithdraw(eventBody.MessageId);
                    DodoBot<TP>.SendChannelMessage("非法文件", eventBody.ChannelId);
                    return;
                }
                using var client = new HttpClient();
                var downloadBytes = client.GetByteArrayAsync(messageBodyFile.Url).Result;
                var pkms = FileTradeHelper<TP>.Bin2List(downloadBytes);
                ProcessWithdraw(eventBody.MessageId);
                if (pkms.Count == 1) 
                    new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartTradePKM(pkms[0]);
                else if (pkms.Count > 1 && pkms.Count <= FileTradeHelper<TP>.MaxCountInBin) 
                    new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartTradeMultiPKM(pkms);
                else
                    DodoBot<TP>.SendChannelMessage("文件内容不正确", eventBody.ChannelId);
                return;
            }

            if (eventBody.MessageBody is not MessageBodyText messageBodyText) return;

            var content = messageBodyText.Content;

            LogUtil.LogInfo($"{eventBody.Personal.NickName}({eventBody.DodoSourceId}):{content}", LogIdentity);
            if (_botDodoSourceId == null)
            {
                _botDodoSourceId = _openApiService.GetBotInfo(new GetBotInfoInput()).DodoSourceId;
            }
            if (!content.Contains($"<@!{_botDodoSourceId}>")) return;

            content = content.Substring(content.IndexOf('>') + 1);
            if (typeof(TP) == typeof(PK9) && content.Contains("\n\n") && ShowdownTranslator<TP>.IsPS(content))// 仅SV支持批量，其他偷懒还没写
            {
                ProcessWithdraw(eventBody.MessageId);
                new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartTradeMultiPs(content.Trim());
                return;
            }
            else if (ShowdownTranslator<TP>.IsPS(content))
            {
                ProcessWithdraw(eventBody.MessageId);
                new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartTradePs(content.Trim());
                return;
            }
            else if (content.Trim().StartsWith("dump"))
            {
                ProcessWithdraw(eventBody.MessageId);
                new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartDump();
                return;
            }
            else if (typeof(TP) == typeof(PK9) && content.Trim().Contains('+'))// 仅SV支持批量，其他偷懒还没写
            {
                ProcessWithdraw(eventBody.MessageId);
                new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartTradeMultiChinesePs(content.Trim());
                return;
            }

            var ps = ShowdownTranslator<TP>.Chinese2Showdown(content);
            if (!string.IsNullOrWhiteSpace(ps))
            {
                LogUtil.LogInfo($"收到命令\n{ps}", LogIdentity);
                ProcessWithdraw(eventBody.MessageId);
                new DodoTrade<TP>(ulong.Parse(eventBody.DodoSourceId), eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId).StartTradePs(ps);
            }
            else if (content.Contains("取消"))
            {
                var result = DodoBot<TP>.Info.ClearTrade(ulong.Parse(eventBody.DodoSourceId));
                DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoSourceId), $" {GetClearTradeMessage(result)}",
                    eventBody.ChannelId);
            }
            else if (content.Contains("位置"))
            {
                var result = DodoBot<TP>.Info.CheckPosition(ulong.Parse(eventBody.DodoSourceId));
                DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoSourceId),
                    $" {GetQueueCheckResultMessage(result)}",
                    eventBody.ChannelId);
            }
            else
            {
                DodoBot<TP>.SendChannelMessage($"{Welcome}", eventBody.ChannelId);
            }
        }

        public string GetQueueCheckResultMessage(QueueCheckResult<TP> result)
        {
            if (!result.InQueue || result.Detail is null)
                return "你不在队列里";
            var msg = $"你在第{result.Position}位";
            var pk = result.Detail.Trade.TradeData;
            if (pk.Species != 0)
                msg += $"，交换宝可梦：{ShowdownTranslator<TP>.GameStringsZh.Species[result.Detail.Trade.TradeData.Species]}";
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

        private void ProcessWithdraw(string messageId)
        {
            if (_dodoSettings.WithdrawTradeMessage)
            {
                DodoBot<TP>.OpenApiService.SetChannelMessageWithdraw(new SetChannelMessageWithdrawInput() { MessageId = messageId }, true);
            }  
        }

        public override void MessageReactionEvent(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyMessageReaction>> input)
        {
            // Do nothing
        }

    }
}