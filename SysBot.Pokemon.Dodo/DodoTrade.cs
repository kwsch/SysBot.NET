using PKHeX.Core;
using SysBot.Pokemon.Helpers;
using System;

namespace SysBot.Pokemon.Dodo
{
    public class DodoTrade<T> : AbstractTrade<T> where T : PKM, new()
    {
        private readonly string channelId = default!;
        private readonly string islandSourceId = default!;

        public DodoTrade(ulong dodoId, string nickName, string channelId, string islandSourceId)
        {
            SetPokeTradeTrainerInfo(new PokeTradeTrainerInfo(nickName, dodoId));
            SetTradeQueueInfo(DodoBot<T>.Info);
            this.channelId = channelId;
            this.islandSourceId = islandSourceId;
        }

        public override IPokeTradeNotifier<T> GetPokeTradeNotifier(T pkm, int code)
        {
            return new DodoTradeNotifier<T>(pkm, userInfo, code, userInfo.ID.ToString(), channelId, islandSourceId);
        }

        public override void SendMessage(string message)
        {
            DodoBot<T>.SendChannelAtMessage(userInfo.ID, message, channelId);
        }
    }
}
