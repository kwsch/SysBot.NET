namespace SysBot.Pokemon
{
    public enum PokeTradeResult
    {
        Success,

        // Trade Partner Failures
        NoTrainerFound,
        TrainerTooSlow,
        IllegalTrade,

        // Recovery -- General Bot Failures
        // Anything below here should be retried once if possible.
        Aborted,
        RecoverStart,
        RecoverPostLinkCode,
        RecoverOpenBox,
        RecoverReturnOverworld,
    }

    public static class PokeTradeResultExtensions
    {
        public static bool AttemptRetry(this PokeTradeResult t) => t != PokeTradeResult.Success && t >= PokeTradeResult.Aborted;
    }
}