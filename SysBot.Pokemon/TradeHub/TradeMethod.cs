namespace SysBot.Pokemon
{
    /// <summary>
    /// Differentiates the different types of player initiated in-game trades.
    /// </summary>
    public enum TradeMethod
    {
        /// <summary>
        /// Trades between specific players
        /// </summary>
        LinkTrade,

        /// <summary>
        /// Trades between randomly matched players
        /// </summary>
        SurpriseTrade,
    }
}
