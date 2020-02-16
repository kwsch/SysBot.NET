using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    /// <summary>
    /// Commands a Bot to a perform a routine asynchronously.
    /// </summary>
    public abstract class SwitchRoutineExecutor<T> where T : SwitchBotConfig
    {
        public readonly SwitchConnectionAsync Connection;
        public readonly T Config;

        protected SwitchRoutineExecutor(T cfg)
        {
            Config = cfg;
            Connection = new SwitchConnectionAsync(cfg.IP, cfg.Port);
        }

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        public async Task RunAsync(CancellationToken token)
        {
            Connection.Connect();
            await MainLoop(token).ConfigureAwait(false);
            Connection.Disconnect();
        }

        protected abstract Task MainLoop(CancellationToken token);

        public async Task Click(SwitchButton b, int delay, CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.Click(b), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task SetStick(SwitchStick stick, int x, int y, int delay, CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.SetStick(stick, (short)x, (short)y), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task DetachController(CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.DetachController(), token).ConfigureAwait(false);
        }
    }
}