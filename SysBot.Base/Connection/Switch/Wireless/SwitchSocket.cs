using System.Net.Sockets;

namespace SysBot.Base
{
    /// <summary>
    /// Abstract class representing the communication over a WiFi socket.
    /// </summary>
    public abstract class SwitchSocket : IConsoleConnection
    {
        protected Socket Connection;
        protected readonly IWirelessConnectionConfig Info;

        public string Name { get; }
        public string Label { get; set; }
        public bool Connected { get; protected set; }

        protected SwitchSocket(IWirelessConnectionConfig wi, SocketType type = SocketType.Stream, ProtocolType proto = ProtocolType.Tcp)
        {
            Connection = new Socket(type, proto);
            Info = wi;
            Name = Label = wi.IP;
        }

        public void Log(string message) => LogInfo(message);
        public void LogInfo(string message) => LogUtil.LogInfo(message, Label);
        public void LogError(string message) => LogUtil.LogError(message, Label);

        public abstract void Connect();
        public abstract void Reset();
        public abstract void Disconnect();
    }
}
