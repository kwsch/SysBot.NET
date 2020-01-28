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

    public static class TradeUtil
    {
        public static int GetCodeDigit(int code, int c)
        {
            for (int i = 0; i < c; i++)
                code /= 10;
            return code % 10;
        }
    }
}