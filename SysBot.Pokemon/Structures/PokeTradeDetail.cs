using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> where TPoke : PKM
    {
        public int Code;
        public TPoke TradeData;
        public PokeTradeTrainerInfo Trainer;

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, int code = -1)
        {
            Code = code;
            TradeData = pkm;
            Trainer = info;
        }
    }
}