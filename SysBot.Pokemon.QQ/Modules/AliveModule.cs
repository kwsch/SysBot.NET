using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Modules;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using PKHeX.Core;
using System.Linq;
using System.Reactive.Linq;

namespace SysBot.Pokemon.QQ
{
    public class AliveModule<T> : IModule where T : PKM, new()
    {
        public bool? IsEnable { get; set; } = true;

        public async void Execute(MessageReceiverBase @base)
        {
            QQSettings settings = MiraiQQBot<T>.Settings;

            var receiver = @base.Concretize<GroupMessageReceiver>();
            if (settings.AliveMsg == receiver.MessageChain.OfType<PlainMessage>()?.FirstOrDefault()?.Text)
            {
                await MessageManager.SendGroupMessageAsync(receiver.Sender.Group.Id, settings.AliveMsg);
                return;
            }
        }
    }
}
