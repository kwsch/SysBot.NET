namespace SysBot.Pokemon
{
    public static class TradeUtil
    {
        public static int GetCodeDigit(int code, int c)
        {
            for (int i = 3; i > c; i--)
                code /= 10;
            return code % 10;
        }
    }
}