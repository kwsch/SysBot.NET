using System;

namespace SysBot.Base;

/// <summary>
/// Forward log messages to another location.
/// </summary>
public interface ILogForwarder
{
    /// <summary>
    /// Forward a log message.
    /// </summary>
    /// <param name="message">Message to forward.</param>
    /// <param name="identity">Identity of the source.</param>
    void Forward(string message, string identity);
}
