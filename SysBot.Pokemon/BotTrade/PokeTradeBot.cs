using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchCommand;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class PokeTradeBot
    {
        private readonly SwitchBot Bot;
        public readonly PokeTradeQueue<PK8> Pool;
        private const int MyGiftAddress = 0x4293D8B0;
        private const int ReadPartyFormatPokeSize = 0x158;
        public string? DumpFolder { get; set; }

        public PokeTradeBot(PokeTradeQueue<PK8> queue, string ip, int port)
        {
            Bot = new SwitchBot(ip, port);
            Pool = queue;
        }

        public PokeTradeBot(PokeTradeQueue<PK8> queue, SwitchBotConfig cfg) : this(queue, cfg.IP, cfg.Port) { }

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        public async Task RunAsync(CancellationToken token)
        {
            await Bot.Connect().ConfigureAwait(false);
            await MainLoop(token).ConfigureAwait(false);
            await Bot.Disconnect().ConfigureAwait(false);
        }

        private async Task MainLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!Pool.TryDequeue(out var poke))
                {
                    await Task.Delay(10_000, token).ConfigureAwait(false);
                    continue;
                }

                var pkm = poke.TradeData;
                var edata = pkm.EncryptedPartyData;
                await Bot.Send(Poke(MyGiftAddress, edata), token).ConfigureAwait(false);

                // load up y comm
                await Bot.Send(Click(Y), token).ConfigureAwait(false);
                await Task.Delay(1_000, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                // TODO: Do trade with detail
                var code = poke.Code;
                if (code < 0)
                    code = Util.Rand.Next(8000, 8400);

                for (int i = 0; i < 4; i++)
                {
                    var digit = TradeUtil.GetCodeDigit(code, i);
                    // enter code digit
                }

                await WaitForTradeToFinish(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                await ReadDumpB1S1(token).ConfigureAwait(false);
            }
        }

        private async Task ReadDumpB1S1(CancellationToken token)
        {
            if (DumpFolder == null)
                return;

            // get pokemon from box1slot1
            var data = await Bot.ReadBytes(MyGiftAddress, ReadPartyFormatPokeSize, token).ConfigureAwait(false);
            var pk8 = new PK8(data);
            File.WriteAllBytes(Path.Combine(DumpFolder, Util.CleanFileName(pk8.FileName)), pk8.DecryptedPartyData);
        }

        private static async Task WaitForTradeToFinish(CancellationToken token)
        {
            // probably needs to be longer for trade evolutions
            await Task.Delay(30_000, token).ConfigureAwait(false);
        }
    }
}
