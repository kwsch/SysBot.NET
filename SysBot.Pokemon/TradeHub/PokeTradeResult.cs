namespace SysBot.Pokemon
{
    public enum PokeTradeResult
    {
        Success,

        // Trade Partner Failures
        NoTrainerFound,
        TrainerTooSlow,
        TrainerRequestBad,
        IllegalTrade,
        SuspiciousActivity,

        // Recovery -- General Bot Failures
        // Anything below here should be retried once if possible.
        RoutineCancel,
        ExceptionConnection,
        ExceptionInternal,
        RecoverStart,
        RecoverPostLinkCode,
        RecoverOpenBox,
        RecoverReturnOverworld,
    }

    public static class PokeTradeResultExtensions
    {
        public static bool ShouldAttemptRetry(this PokeTradeResult t) => t >= PokeTradeResult.RoutineCancel;
    }
}
