using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor
    {
        public readonly PokeTradeHub<PK8> Hub;
        private const int MyGiftAddress = 0x4293D8B0;
        private const int ReadPartyFormatPokeSize = 0x158;
        public string? DumpFolder { get; set; }

        public PokeTradeBot(PokeTradeHub<PK8> hub, string ip, int port) : base(ip, port) => Hub = hub;
        public PokeTradeBot(PokeTradeHub<PK8> hub, SwitchBotConfig cfg) : this(hub, cfg.IP, cfg.Port) { }

        protected override async Task MainLoop(CancellationToken token)
        {
            bool waitBarrier = false;
            while (!token.IsCancellationRequested)
            {
                if (!Hub.Queue.TryDequeue(out var poke))
                {
                    Connection.Log("Waiting for new trade data. Sleeping for a bit.");
                    await Task.Delay(10_000, token).ConfigureAwait(false);
                    continue;
                }

                Connection.Log("Starting next trade. Getting data...");
                // Update Barrier Settings
                waitBarrier = UpdateBarrier(Hub.Barrier, poke.IsRandomCode, waitBarrier);
                var pkm = poke.TradeData;
                await Connection.WriteBytesAsync(pkm.EncryptedPartyData, MyGiftAddress, token).ConfigureAwait(false);

                // load up y comm
                await Click(Y, 1_000, token).ConfigureAwait(false);

                // Select Link Trade Trade
                await Click(A, 1_000, token).ConfigureAwait(false);

                // Select Password
                await Click(DDOWN, 200, token).ConfigureAwait(false);

                for (int i = 0; i < 2; i++)
                    await Click(A, 2_000, token).ConfigureAwait(false);

                // Loading Screen
                await Task.Delay(2_000, token).ConfigureAwait(false);

                // Enter Code
                var code = poke.Code;
                if (code < 0)
                    code = Hub.GetRandomTradeCode();
                await EnterTradeCode(code, token).ConfigureAwait(false);

                // Wait for Barrier to trigger all bots simultaneously.
                if (waitBarrier && Hub.UseBarrier)
                    Hub.Barrier.SignalAndWait(TimeSpan.FromSeconds(60), token);
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

                Connection.Log("Trade complete!");
                Hub.AddCompletedTrade();
                await ReadDumpB1S1(token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks if the barrier needs to get updated to consider this bot.
        /// If it should be considered, it adds it to the barrier if it is not already added.
        /// If it should not be considered, it removes it from the barrier if not already removed.
        /// </summary>
        private static bool UpdateBarrier(Barrier b, bool shouldWait, bool alreadyWait)
        {
            if (shouldWait)
            {
                if (!alreadyWait)
                    b.AddParticipant();
                return true;
            }
            if (alreadyWait)
                b.RemoveParticipant();
            return false;
        }

        private async Task ReadDumpB1S1(CancellationToken token)
        {
            if (DumpFolder == null)
                return;

            // get pokemon from box1slot1
            var data = await Connection.ReadBytesAsync(MyGiftAddress, ReadPartyFormatPokeSize, token).ConfigureAwait(false);
            var pk8 = new PK8(data);
            File.WriteAllBytes(Path.Combine(DumpFolder, Util.CleanFileName(pk8.FileName)), pk8.DecryptedPartyData);
        }
    }
}
