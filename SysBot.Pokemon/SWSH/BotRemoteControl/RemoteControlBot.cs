using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public class RemoteControlBot : PokeRoutineExecutor8
    {
        public RemoteControlBot(PokeBotState cfg) : base(cfg)
        {
        }

        public override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main loop, then waiting for commands.");
            Config.IterateNextRoutine();
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                ReportStatus();
            }
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            await SetStick(SwitchStick.LEFT, 0, 0, 0_500, CancellationToken.None).ConfigureAwait(false); // reset
            await CleanExit(new DummyReset(), CancellationToken.None).ConfigureAwait(false);
        }

        private class DummyReset : IBotStateSettings
        {
            public bool ScreenOff => true;
        }
    }
}
