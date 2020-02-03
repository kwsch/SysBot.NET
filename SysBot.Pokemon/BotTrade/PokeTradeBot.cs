using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> Hub;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        public string? DumpFolder { get; set; }

        /// <summary>
        /// Synchronized start for multiple bots.
        /// </summary>
        public bool ShouldWaitAtBarrier { get; private set; }

        public PokeTradeBot(PokeTradeHub<PK8> hub, string ip, int port) : base(ip, port) => Hub = hub;
        public PokeTradeBot(PokeTradeHub<PK8> hub, SwitchBotConfig cfg) : this(hub, cfg.IP, cfg.Port) { }

        protected override async Task MainLoop(CancellationToken token)
        {
            // Initialize bot information
            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
            Connection.Name = $"{sav.OT}-{sav.DisplayTID}";

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
                ShouldWaitAtBarrier = UpdateBarrier(Hub.Barrier, poke.IsRandomCode, ShouldWaitAtBarrier);
                var pkm = poke.TradeData;
                await Connection.WriteBytesAsync(pkm.EncryptedPartyData, Box1Slot1, token).ConfigureAwait(false);

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
                if (ShouldWaitAtBarrier && Hub.UseBarrier)
                    Hub.Barrier.SignalAndWait(TimeSpan.FromSeconds(60), token);
                await Click(PLUS, 0_100, token).ConfigureAwait(false);

                // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
                // Confirming...
                for (int i = 0; i < 4; i++)
                    await Click(A, 2_000, token).ConfigureAwait(false);

                await Task.Delay(Util.Rand.Next(100, 1000), token).ConfigureAwait(false);

                await Click(A, 1_000, token).ConfigureAwait(false);

                // Wait 40 Seconds for Trainer...
                await Connection.WriteBytesAsync(new byte[344], ShownTradeDataOffset, token).ConfigureAwait(false);
                var partnerFound = await ReadUntilChanged(ShownTradeDataOffset, new byte[4], 40_000, 2_000, token).ConfigureAwait(false);

                if (!partnerFound)
                    break; // Error handling here!

                // Potential Trainer Found!

                // Select Pokemon
                // pkm already injected to b1s1
                await Task.Delay(300, token).ConfigureAwait(false);

                await Click(A, 2_000, token).ConfigureAwait(false);
                await Click(A, 2_000, token).ConfigureAwait(false);

                // Wait for User Input...
                var pk = await ReadUntilPresent(ShownTradeDataOffset, 25_000, 1_000, token).ConfigureAwait(false);
                if (pk == null)
                    break; // Error handling here!

                await Click(A, 3_000, token).ConfigureAwait(false);
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
                await ReadDumpB1S1(DumpFolder, token).ConfigureAwait(false);
            }

            ExitRoutine();
        }

        private void ExitRoutine()
        {
            // On exit, remove self from Barrier list
            if (ShouldWaitAtBarrier)
            {
                Hub.Barrier.RemoveParticipant();
                ShouldWaitAtBarrier = false;
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
    }
}
