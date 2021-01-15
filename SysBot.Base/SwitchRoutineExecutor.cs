using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Commands a Bot to a perform a routine asynchronously.
    /// </summary>
    public abstract class RoutineExecutor<T> : IRoutineExecutor where T : class, IConsoleBotConfig
    {
        public readonly IConsoleConnectionAsync Connection;
        public readonly T Config;

        protected RoutineExecutor(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> cfg)
        {
            Config = (T)cfg;
            Connection = cfg.CreateAsynchronous();
        }

        public string LastLogged { get; private set; } = "Not Started";
        public DateTime LastTime { get; private set; } = DateTime.Now;

        public void ReportStatus() => LastTime = DateTime.Now;

        public void Log(string message)
        {
            Connection.Log(message);
            LastLogged = message;
            LastTime = DateTime.Now;
        }

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        public async Task RunAsync(CancellationToken token)
        {
            Connection.Connect();
            Log("Initializing connection with console...");
            await EchoCommands(false, token).ConfigureAwait(false);
            await MainLoop(token).ConfigureAwait(false);
            Connection.Disconnect();
        }

        public abstract Task MainLoop(CancellationToken token);
        public abstract void SoftStop();

        public async Task Click(SwitchButton b, int delay, CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.Click(b), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task PressAndHold(SwitchButton b, int hold, int delay, CancellationToken token)
        {
            // Set hold delay
            var delaycgf = SwitchCommand.Configure(SwitchConfigureParameter.buttonClickSleepTime, hold);
            await Connection.SendAsync(delaycgf, token).ConfigureAwait(false);
            // Press the button
            await Click(b, delay, token).ConfigureAwait(false);
            // Reset delay
            delaycgf = SwitchCommand.Configure(SwitchConfigureParameter.buttonClickSleepTime, 50); // 50 ms
            await Connection.SendAsync(delaycgf, token).ConfigureAwait(false);
        }

        public async Task DaisyChainCommands(int Delay, SwitchButton[] buttons, CancellationToken token)
        {
            SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, Delay);
            var commands = buttons.Select(SwitchCommand.Click).ToArray();
            var chain = commands.SelectMany(x => x).ToArray();
            await Connection.SendAsync(chain, token).ConfigureAwait(false);
            SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, 0);
        }

        public async Task SetStick(SwitchStick stick, short x, short y, int delay, CancellationToken token)
        {
            var cmd = SwitchCommand.SetStick(stick, x, y);
            await Connection.SendAsync(cmd, token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task DetachController(CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.DetachController(), token).ConfigureAwait(false);
        }

        public async Task EchoCommands(bool value, CancellationToken token)
        {
            var cmd = SwitchCommand.Configure(SwitchConfigureParameter.echoCommands, value ? 1 : 0);
            await Connection.SendAsync(cmd, token).ConfigureAwait(false);
        }
    }
}
