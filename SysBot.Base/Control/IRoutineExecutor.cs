using System;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base;

public interface IRoutineExecutor
{
    string LastLogged { get; }

    DateTime LastTime { get; }

    string GetSummary();

    Task InitialStartup(CancellationToken token);

    void Log(string message);

    Task MainLoop(CancellationToken token);

    void ReportStatus();

    /// <summary>
    /// Connects to the console, then runs the bot.
    /// </summary>
    /// <param name="token">Cancel this token to have the bot stop looping.</param>
    Task RunAsync(CancellationToken token);

    void SoftStop();
}
