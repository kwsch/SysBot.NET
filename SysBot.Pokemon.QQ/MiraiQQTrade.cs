using Mirai.Net.Utils.Scaffolds;
using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SysBot.Pokemon.QQ
{
    public class MiraiQQTrade<T> : AbstractTrade<T> where T : PKM, new()
    {
        private readonly string GroupId = default!;

        public MiraiQQTrade(string qq, string nickName) 
        {
            SetPokeTradeTrainerInfo(new PokeTradeTrainerInfo(nickName, ulong.Parse(qq)));
            SetTradeQueueInfo(MiraiQQBot<T>.Info);
            GroupId = MiraiQQBot<T>.Settings.GroupId;
        }
        public override IPokeTradeNotifier<T> GetPokeTradeNotifier(T pkm, int code)
        {
            return new MiraiQQTradeNotifier<T>(pkm, userInfo, code, userInfo.TrainerName, GroupId);
        }

        public override void SendMessage(string message)
        {
            MiraiQQBot<T>.SendGroupMessage(new MessageChainBuilder().At(userInfo.ID.ToString()).Plain(message).Build());
        }
    }
}
