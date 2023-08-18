using PKHeX.Core;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Stores data for indicating how a queue position/presence check resulted.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed record QueueCheckResult<T> where T : PKM, new()
    {
        public readonly bool InQueue;
        public readonly TradeEntry<T>? Detail;
        public readonly int Position;
        public readonly int QueueCount;

        public static readonly QueueCheckResult<T> None = new();

        public QueueCheckResult(bool inQueue = false, TradeEntry<T>? detail = default, int position = -1, int queueCount = -1)
        {
            InQueue = inQueue;
            Detail = detail;
            Position = position;
            QueueCount = queueCount;
        }

        public string GetMessage()
        {
            if (!InQueue || Detail is null)
                return "You are not in the queue.";
            var position = $"{Position}/{QueueCount}";
            var msg = $"You are in the {Detail.Type} queue! Position: {position} (ID {Detail.Trade.ID})";
            var pk = Detail.Trade.TradeData;
            if (pk.Species != 0)
                msg += $", 받는 포켓몬: {GameInfo.GetStrings(1).Species[pk.Species]}";
            return msg;
        }
    }
}