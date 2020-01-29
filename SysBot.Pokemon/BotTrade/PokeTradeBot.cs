using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchCommand;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor
    {
        public readonly PokeTradeQueue<PK8> Pool;
        private const int MyGiftAddress = 0x4293D8B0;
        private const int ReadPartyFormatPokeSize = 0x158;
        public string? DumpFolder { get; set; }

        public PokeTradeBot(PokeTradeQueue<PK8> queue, string ip, int port) : base(ip, port) => Pool = queue;
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
                    Bot.Log("Waiting for new trade data. Sleeping for a bit.");
                    await Task.Delay(10_000, token).ConfigureAwait(false);
                    continue;
                }

                Bot.Log("Starting next trade. Getting data...");
                var pkm = poke.TradeData;
                var edata = pkm.EncryptedPartyData;
                await Bot.Send(Poke(MyGiftAddress, edata), token).ConfigureAwait(false);

                // load up y comm
                await Click(Y, 1_000, token).ConfigureAwait(false);

                // Select Link Trade Trade
                await Click(A, 1_000, token).ConfigureAwait(false);

                // Select Password
                await Click(DDOWN, 50, token).ConfigureAwait(false);

                for (int i = 0; i < 2; i++)
                    await Click(A, 1_000, token).ConfigureAwait(false);

                // Loading Screen
                await Task.Delay(2_000, token).ConfigureAwait(false);

                // Enter Code
                var code = poke.Code;
                if (code < 0)
                    code = Util.Rand.Next(8000, 8400);
                await SelectTradeCode(code, token).ConfigureAwait(false);

                await Click(PLUS, 0_100, token).ConfigureAwait(false);

                // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
                // Confirming...
                for (int i = 0; i < 4; i++)
                    await Click(A, 2_000, token).ConfigureAwait(false);

                await Task.Delay(Util.Rand.Next(100, 1000), token).ConfigureAwait(false);

                await Click(A, 1_000, token).ConfigureAwait(false);

                // Wait 30 Seconds for Trainer...
                await Task.Delay(40_000, token).ConfigureAwait(false);

                // Potential Trainer Found!

                // Select Pokemon
                // pkm already injected to b1s1
                await Task.Delay(300, token).ConfigureAwait(false);

                await Click(A, 2_000, token).ConfigureAwait(false);
                await Click(A, 2_000, token).ConfigureAwait(false);

                // Wait for User Input...
                for (int i = 0; i < 25; i++)
                    await Click(A, 1_000, token).ConfigureAwait(false);

                await Click(A, 3_000, token).ConfigureAwait(false);

                // Link Trade Started, wait 10 seconds, abort after 15 Seconds!
                for (int i = 0; i < 3; i++)
                    await Click(B, 1_000, token).ConfigureAwait(false);

                for (int i = 0; i < 3; i++)
                    await Click(A, 1_500, token).ConfigureAwait(false);

                // Wait 30 Seconds until Trade is finished...
                await Task.Delay(30_000 + Util.Rand.Next(500, 5000), token).ConfigureAwait(false);

                // Pokemon has probably arrived, bypass Trade Evolution...
                await Click(Y, 1_000, token).ConfigureAwait(false);

                await Task.Delay(1_000 + Util.Rand.Next(500, 5000), token).ConfigureAwait(false);

                for (int i = 0; i < 10; i++)
                {
                    await Click(B, 1_000, token).ConfigureAwait(false);
                    await Click(B, 1_000, token).ConfigureAwait(false);
                    await Click(A, 1_000, token).ConfigureAwait(false);
                }

                for (int i = 0; i < 3; i++)
                    await Click(A, 1_000, token).ConfigureAwait(false);

                // Spam A Button in Case of Trade Evolution/Moves Learning/Dex
                for (int i = 0; i < 20; i++)
                    await Click(A, 1_000, token).ConfigureAwait(false);

                for (int i = 0; i < 3; i++)
                    await Click(B, 1_000, token).ConfigureAwait(false);

                // Trade was Successful!
                if (token.IsCancellationRequested)
                    break;

                Bot.Log("Trade complete!");
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
    }
}
