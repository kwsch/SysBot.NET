using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Base
{
    public class BotSource<T> where T : SwitchBotConfig
    {
        public readonly SwitchRoutineExecutor<T> Bot;
        private CancellationTokenSource Source = new CancellationTokenSource();

        public BotSource(SwitchRoutineExecutor<T> bot) => Bot = bot;

        public bool IsRunning { get; private set; }

        public void Stop()
        {
            Source.Cancel();
            Source = new CancellationTokenSource();

            // Detach Controllers
            Task.Run(() => Bot.Connection.SendAsync(SwitchCommand.DetachController(), CancellationToken.None));
            IsRunning = false;
        }

        public void Pause()
        {
            Bot.SoftStop();
        }

        public void Start()
        {
            if (IsRunning)
                Stop();
            Task.Run(() => Bot.RunAsync(Source.Token), Source.Token);
            IsRunning = true;
        }

        public void Resume()
        {
            Start();
        }
    }
}