namespace SysBot.Base
{
    /// <summary>
    /// Contains details for communicating with another wireless device.
    /// </summary>
    public interface IWirelessConnectionConfig
    {
        /// <summary> IP Address (X.X.X.X) </summary>
        string IP { get; set; }

        /// <summary> Port </summary>
        int Port { get; set; }
    }
}
