using System;

namespace SysBot.Base;

/// <summary>
/// Forward log messages to the console.
/// </summary>
public sealed class ConsoleForwarder : ILogForwarder
{
    /// <summary>
    /// Singleton instance of the console forwarder.
    /// </summary>
    public static readonly ConsoleForwarder Instance = new();

    public void Forward(string message, string identity)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] - {identity}: {message}{Environment.NewLine}";
        Console.WriteLine(line);
    }
}
