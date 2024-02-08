using Mirai.Net.Data.Messages;
using Mirai.Net.Modules;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SysBot.Pokemon.QQ
{
    public class MiraiQQBot<T> where T : PKM, new()
    {
        private static PokeTradeHub<T> Hub = default!;

        internal static TradeQueueInfo<T> Info => Hub.Queues.Info;
        private readonly MiraiBot Client;

        internal static QQSettings Settings = default!;

        public MiraiQQBot(QQSettings settings, PokeTradeHub<T> hub)
        {
            Settings = settings;
            Hub = hub;
            Client = new MiraiBot
            {
                Address = settings.Address,
                QQ = settings.QQ,
                VerifyKey = settings.VerifyKey
            };

            var modules = new List<IModule>()
            {
                new AliveModule<T>(),
                new CommandModule<T>(),
                new FileModule<T>(),
                new PsModule<T>()
            };
            Client.MessageReceived.SubscribeGroupMessage(receiver => { if (receiver.GroupId == Settings.GroupId) modules.Raise(receiver); });
            var GroupId = Settings.GroupId;
            Task.Run(async () =>
            {
                try
                {
                    await Client.LaunchAsync();

                    if (!string.IsNullOrWhiteSpace(Settings.MessageStart))
                    {
                        await MessageManager.SendGroupMessageAsync(GroupId, Settings.MessageStart);
                        await Task.Delay(1_000).ConfigureAwait(false);
                    }

                    if (typeof(T) == typeof(PK8))
                    {
                        await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为剑盾");
                    }
                    else if (typeof(T) == typeof(PB8))
                    {
                        await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为晶灿钻石明亮珍珠");
                    }
                    else if (typeof(T) == typeof(PA8))
                    {
                        await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为阿尔宙斯");
                    }
                    else if (typeof(T) == typeof(PK9))
                    {
                        await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为朱紫");
                    }

                    await Task.Delay(1_000).ConfigureAwait(false);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    LogUtil.LogError(ex.Message, nameof(MiraiQQBot<T>));
                }
            });
        }

        public async static void SendGroupMessage(MessageChain mc)
        {
            if (string.IsNullOrEmpty(Settings.GroupId)) return;
            await MessageManager.SendGroupMessageAsync(Settings.GroupId, mc);
        }

        public async static void SendFriendMessage(string friendId, MessageChain mc)
        {
            if (string.IsNullOrEmpty(friendId) || string.IsNullOrEmpty(Settings.GroupId)) return;
            await MessageManager.SendFriendMessageAsync(friendId, mc);
        }

        public async static void SendTempMessage(string friendId, MessageChain mc)
        {
            if (string.IsNullOrEmpty(friendId) || string.IsNullOrEmpty(Settings.GroupId)) return;
            await MessageManager.SendTempMessageAsync(friendId, Settings.GroupId, mc);
        }
    }
}
