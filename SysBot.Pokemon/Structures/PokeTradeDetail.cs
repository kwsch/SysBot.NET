using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> where TPoke : PKM
    {
        public int Code;
        public TPoke TradeData;
        public PokeTradeTrainerInfo Trainer;

        private const int RandomCode = -1;

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, int code = RandomCode)
        {
            Code = code;
            TradeData = pkm;
            Trainer = info;
        }

        public bool IsRandomCode => Code == RandomCode;
    }
}