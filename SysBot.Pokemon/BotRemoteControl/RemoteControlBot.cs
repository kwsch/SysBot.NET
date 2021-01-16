using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon
{
    public class RemoteControlBot : PokeRoutineExecutor
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
        }
    }
}
