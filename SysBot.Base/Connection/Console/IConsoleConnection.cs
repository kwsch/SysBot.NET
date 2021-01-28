namespace SysBot.Base
{
    /// <summary>
    /// Concepts and properties to describe a connection with a console, without explicitly interacting with it.
    /// </summary>
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

        /// <summary>
        /// Connects to the device.
        /// </summary>
        void Connect();

        /// <summary>
        /// Resets the connection to the device, usually by calling <see cref="Disconnect"/> then <see cref="Connect"/> in a clean manner.
        /// </summary>
        void Reset();

        /// <summary>
        /// Disconnects from the device.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Indicates if the device is currently connected.
        /// </summary>
        /// <remarks>Sometimes might not check the connection status, rather if it has been called to <see cref="Connect"/> without <see cref="Disconnect"/> being called yet.</remarks>
        bool Connected { get; }

        /// <summary>
        /// Logs a message for the connection.
        /// </summary>
        /// <param name="message">Anything you want the bot to log.</param>
        abstract void Log(string message);

        /// <summary>
        /// Logs an information message for the connection.
        /// </summary>
        /// <param name="message"></param>
        abstract void LogInfo(string message);

        /// <summary>
        /// Logs an error message for the connection.
        /// </summary>
        /// <param name="message"></param>
        abstract void LogError(string message);

        /// <summary>
        /// Maximum amount of data to be sent in a single packet to the device.
        /// </summary>
        /// <remarks>Whenever the amount of data to be sent exceeds this amount, the data payload is split into smaller chunks.</remarks>
        int MaximumTransferSize { get; set; }

        /// <summary>
        /// Base amount of time (in milliseconds) to wait when sending successive commands.
        /// </summary>
        int BaseDelay { get; set; }

        /// <summary>
        /// Slows down the communication for successive commands by dividing the packet length to get a delay (in milliseconds).
        /// </summary>
        /// <remarks>Set this value >= the <see cref="MaximumTransferSize"/> to result in 0 bonus delay.</remarks>
        int DelayFactor { get; set; }
    }
}
