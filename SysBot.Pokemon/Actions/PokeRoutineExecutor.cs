using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class PokeRoutineExecutor
    {
        public readonly SwitchBot Bot;
        protected PokeRoutineExecutor(string ip, int port) => Bot = new SwitchBot(ip, port);

        public async Task Click(SwitchButton b, int delay, CancellationToken token)
        {
            await Bot.Send(SwitchCommand.Click(b), token).ConfigureAwait(false);
            await Task.Delay(delay, token).ConfigureAwait(false);
        }

        public async Task Click(SwitchButton b, int delayMin, int delayMax, CancellationToken token) =>
            await Click(b, Util.Rand.Next(delayMin, delayMax), token).ConfigureAwait(false);

        protected async Task SelectTradeCode(int code, CancellationToken token)
        {
            for (int i = 0; i < 4; i++)
            {
                // Go to 0
                foreach (var e in arr[0])
                    await Click(e, 100, token).ConfigureAwait(false);

                var digit = TradeUtil.GetCodeDigit(code, i);
                var entry = arr[digit];
                foreach (var e in entry)
                    await Click(e, 100, token).ConfigureAwait(false);

                //Confirm 
                await Click(A, 1_500, token).ConfigureAwait(false);
            }
        }

        private static readonly SwitchButton[][] arr =
        {
            new[] {DDOWN, DDOWN, DDOWN }, // 0
            new[] {DUP, DUP, DUP, DLEFT}, // 1
            new[] {DUP, DUP, DUP,      }, // 2
            new[] {DUP, DUP, DUP,DRIGHT}, // 3
            new[] {DUP, DUP, DLEFT,    }, // 4
            new[] {DUP, DUP,           }, // 5
            new[] {DUP, DUP, DRIGHT,   }, // 6
            new[] {DUP, DLEFT,         }, // 7
            new[] {DUP,                }, // 8
            new[] {DUP, DRIGHT         }, // 9
        };
    }
}