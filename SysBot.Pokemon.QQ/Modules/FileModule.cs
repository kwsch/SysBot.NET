using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Modules;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;

namespace SysBot.Pokemon.QQ
{
    public class FileModule<T> : IModule where T : PKM, new()
    {
        public bool? IsEnable { get; set; } = true;

        public async void Execute(MessageReceiverBase @base)
        {
            QQSettings settings = MiraiQQBot<T>.Settings;

            var receiver = @base.Concretize<GroupMessageReceiver>();

            var senderQQ = receiver.Sender.Id;
            var nickname = receiver.Sender.Name;
            var groupId = receiver.Sender.Group.Id;

            var fileMessage = receiver.MessageChain.OfType<FileMessage>()?.FirstOrDefault();
            if (fileMessage == null) return;
            LogUtil.LogInfo("In file module", nameof(FileModule<T>));
            var fileName = fileMessage.Name;
            if (!FileTradeHelper<T>.ValidFileName(fileName) || !FileTradeHelper<T>.ValidFileSize(fileMessage.Size))
            {
                await MessageManager.SendGroupMessageAsync(groupId, "非法文件");
                return;
            }

            List<T> pkms = default!;
            try
            {
                var f = await FileManager.GetFileAsync(groupId, fileMessage.FileId, true);
                using var client = new HttpClient();
                byte[] data = client.GetByteArrayAsync(f.DownloadInfo.Url).Result;
                pkms = FileTradeHelper<T>.Bin2List(data);
                await FileManager.DeleteFileAsync(groupId, fileMessage.FileId);
            }
            catch (Exception ex)
            {
                LogUtil.LogError(ex.Message, nameof(FileModule<T>));
                return;
            }
            if (pkms.Count > 1 && pkms.Count <= FileTradeHelper<T>.MaxCountInBin)
                new MiraiQQTrade<T>(senderQQ, nickname).StartTradeMultiPKM(pkms);
            else if (pkms.Count == 1)
                new MiraiQQTrade<T>(senderQQ, nickname).StartTradePKM(pkms[0]);
            else
                await MessageManager.SendGroupMessageAsync(groupId, "文件内容不正确");
        }
    }
}
