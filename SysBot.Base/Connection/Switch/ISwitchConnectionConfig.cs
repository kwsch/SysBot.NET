namespace SysBot.Base
{
    /// <summary>
    /// Defines how the Nintendo Switch is to be communicated with.
    /// </summary>
    public interface ISwitchConnectionConfig : IConsoleBotManaged<ISwitchConnectionSync, ISwitchConnectionAsync>
    {
        /// <summary>
        /// Communication Protocol in use
        /// </summary>
        SwitchProtocol Protocol { get; }

        /// <summary>
        /// Determines if the communication uses 0D 0A (CRLF) to end a command.
        /// </summary>
        bool UseCRLF { get; }
    }
}
