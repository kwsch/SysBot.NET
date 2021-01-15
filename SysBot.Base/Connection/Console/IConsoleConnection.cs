namespace SysBot.Base
{
    public interface IConsoleConnection
    {
        /// <summary>
        /// Internal differentiation for the Bot
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Customized Label for the bot (potentially based on in-game values).
        /// </summary>
        string Label { get; set; }

        void Connect();
        void Reset();
        void Disconnect();
        bool Connected { get; }

        abstract void Log(string message);
        abstract void LogInfo(string message);
        abstract void LogError(string message);
    }
}
