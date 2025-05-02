namespace SysBot.Pokemon.Discord.Commands.Bots
{
    public class TradeQueueResult(bool success)
    {
        public bool Success { get; set; } = success;
    }
}
