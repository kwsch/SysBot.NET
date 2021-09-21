using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    public abstract class SwitchRoutineExecutor<T> : RoutineExecutor<T> where T : class, IConsoleBotConfig
    {
        public readonly bool UseCRLF;

        protected SwitchRoutineExecutor(IConsoleBotManaged<IConsoleConnection, IConsoleConnectionAsync> cfg) : base(cfg)
        {
            UseCRLF = cfg.GetInnerConfig() is ISwitchConnectionConfig {UseCRLF: true};
        }

        public override async Task InitialStartup(CancellationToken token) => await EchoCommands(false, token).ConfigureAwait(false);

        public async Task Click(SwitchButton b, int delay, CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.Click(b, UseCRLF), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task PressAndHold(SwitchButton b, int hold, int delay, CancellationToken token)
        {
            // Set hold delay
            var delaycgf = SwitchCommand.Configure(SwitchConfigureParameter.buttonClickSleepTime, hold, UseCRLF);
            await Connection.SendAsync(delaycgf, token).ConfigureAwait(false);
            // Press the button
            await Click(b, delay, token).ConfigureAwait(false);
            // Reset delay
            delaycgf = SwitchCommand.Configure(SwitchConfigureParameter.buttonClickSleepTime, 50, UseCRLF); // 50 ms
            await Connection.SendAsync(delaycgf, token).ConfigureAwait(false);
        }

        public async Task DaisyChainCommands(int Delay, SwitchButton[] buttons, CancellationToken token)
        {
            SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, Delay, UseCRLF);
            var commands = buttons.Select(z => SwitchCommand.Click(z, UseCRLF)).ToArray();
            var chain = commands.SelectMany(x => x).ToArray();
            await Connection.SendAsync(chain, token).ConfigureAwait(false);
            SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, 0, UseCRLF);
        }

        public async Task SetStick(SwitchStick stick, short x, short y, int delay, CancellationToken token)
        {
            var cmd = SwitchCommand.SetStick(stick, x, y, UseCRLF);
            await Connection.SendAsync(cmd, token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task DetachController(CancellationToken token)
        {
            await Connection.SendAsync(SwitchCommand.DetachController(UseCRLF), token).ConfigureAwait(false);
        }

        public async Task EchoCommands(bool value, CancellationToken token)
        {
            var cmd = SwitchCommand.Configure(SwitchConfigureParameter.echoCommands, value ? 1 : 0, UseCRLF);
            await Connection.SendAsync(cmd, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads an offset until it changes to either match or differ from the comparison value.
        /// </summary>
        /// <returns>If <see cref="match"/> is set to true, then the function returns true when the offset matches the given value.<br>Otherwise, it returns true when the offset no longer matches the given value.</br></returns>
        public async Task<bool> ReadUntilChanged(uint offset, byte[] comparison, int waitms, int waitInterval, bool match, CancellationToken token)
        {
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                var result = await Connection.ReadBytesAsync(offset, comparison.Length, token).ConfigureAwait(false);
                if (match == result.SequenceEqual(comparison))
                    return true;

                await Task.Delay(waitInterval, token).ConfigureAwait(false);
            } while (sw.ElapsedMilliseconds < waitms);
            return false;
        }
    }
}
