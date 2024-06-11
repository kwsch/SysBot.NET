using System.Net.Sockets;

namespace SysBot.Base;

/// <summary>
/// Abstract class representing the communication over a Wi-Fi socket.
/// </summary>
public abstract class SwitchSocket : IConsoleConnection
{
    protected readonly IWirelessConnectionConfig Info;

    private readonly ProtocolType Protocol;

    private readonly SocketType Type;

    protected SwitchSocket(IWirelessConnectionConfig wi, SocketType type = SocketType.Stream, ProtocolType protocol = ProtocolType.Tcp)
    {
        Type = type;
        Protocol = protocol;
        Connection = new Socket(type, protocol);
        Info = wi;
        Name = Label = wi.IP;
    }

    public int BaseDelay { get; set; } = 64;

    public bool Connected => Connection.Connected;

    public int DelayFactor { get; set; } = 256;

    public string Label { get; set; }

    public int MaximumTransferSize { get; set; } = 0x1C0;

    public string Name { get; }

    protected Socket Connection { get; private set; }

    public abstract void Connect();

    public abstract void Disconnect();

    public void InitializeSocket() => Connection = new Socket(Type, Protocol);

    public void Log(string message) => LogInfo(message);

    public void LogError(string message) => LogUtil.LogError(message, Label);

    public void LogInfo(string message) => LogUtil.LogInfo(message, Label);

    public abstract void Reset();
}
