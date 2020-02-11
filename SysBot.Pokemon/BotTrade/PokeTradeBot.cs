using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor, IDumper
    {
        private readonly PokeTradeHub<PK8> Hub;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        public string DumpFolder { get; set; } = string.Empty;

        /// <summary>
        /// Determines if it should dump received trade data.
        /// </summary>
        /// <remarks>If false, will skip dumping.</remarks>
        public bool Dump { get; set; } = true;

        /// <summary>
        /// Synchronized start for multiple bots.
        /// </summary>
        public bool ShouldWaitAtBarrier { get; private set; }

        public PokeTradeBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg) => Hub = hub;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        protected override async Task MainLoop(CancellationToken token)
        {
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            Hub.Bots.Add(this);
            while (!token.IsCancellationRequested)
            {
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    PokeRoutineType.LinkTrade => DoLinkTrades(sav, token),
                    PokeRoutineType.SurpriseTrade => DoSurpriseTrades(sav, token),
                    PokeRoutineType.DuduBot => DoDuduTrades(token),
                    _ => DoNothing(token),
                };
                await task.ConfigureAwait(false);
            }
            Hub.Bots.Remove(this);
        }

        private async Task DoNothing(CancellationToken token)
        {
            bool waiting = false;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (!waiting)
                    Connection.Log("No task assigned. Waiting for new task assignment.");
                waiting = true;
                await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task DoLinkTrades(SAV8SWSH sav, CancellationToken token)
        {
            bool waiting = false;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.LinkTrade)
            {
                if (!waiting)
                    Connection.Log("Starting next Link Code trade. Getting data...");

                if (!Hub.Queue.TryDequeue(out var poke, out var priority))
                {
                    if (!waiting)
                        Connection.Log("Nothing to send in the queue! Waiting for new trade data.");
                    waiting = true;
                    await Task.Delay(1_000, token).ConfigureAwait(false);
                    continue;
                }

                waiting = false;
                await EnsureConnectedToYCom(token).ConfigureAwait(false);
                var result = await PerformLinkCodeTrade(sav, poke, token).ConfigureAwait(false);
                if (result != PokeTradeResult.Success) // requeue
                {
                    poke.TradeCanceled(this, result);
                    if (result == PokeTradeResult.Aborted)
                        Hub.Queue.Enqueue(poke, priority);
                }
            }

            ExitLinkTradeRoutine();
        }

        private async Task DoSurpriseTrades(SAV8SWSH sav, CancellationToken token)
        {
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.SurpriseTrade)
            {
                var pkm = Hub.Pool.GetRandomPoke();
                await EnsureConnectedToYCom(token).ConfigureAwait(false);
                var _ = await PerformSurpriseTrade(sav, pkm, token).ConfigureAwait(false);
            }
        }

        private async Task DoDuduTrades(CancellationToken token)
        {
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.DuduBot)
            {
                await EnsureConnectedToYCom(token).ConfigureAwait(false);
                var _ = await PerformDuduTrade(token).ConfigureAwait(false);
            }
        }

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8SWSH sav, PokeTradeDetail<PK8> poke, CancellationToken token)
        {
            /*
             * TODO Add Screen Detection checks.
             *await SetupScreenDetection(token);
            */
            await Task.Delay(5_000, token).ConfigureAwait(false);
            var _ = await SetupScreenDetection(token).ConfigureAwait(false);
            poke.TradeInitialize(this);
            // Update Barrier Settings
            ShouldWaitAtBarrier = UpdateBarrier(Hub.Barrier, poke.IsRandomCode, ShouldWaitAtBarrier);
            var pkm = poke.TradeData;
            await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            Connection.Log("Open Y-COM Menu");
            await Click(Y, 1_000, token).ConfigureAwait(false);

            Connection.Log("Select Link Trade");
            await Click(A, 1_000, token).ConfigureAwait(false);

            Connection.Log("Select Link Trade Code");
            await Click(DDOWN, 200, token).ConfigureAwait(false);

            for (int i = 0; i < 2; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            // Loading Screen
            await Task.Delay(2_000, token).ConfigureAwait(false);

            var code = poke.Code;
            if (code < 0)
                code = Hub.Config.GetRandomTradeCode();
            Connection.Log($"Entering Link Trade Code: {code} ...");
            await EnterTradeCode(code, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            if (ShouldWaitAtBarrier && Hub.UseBarrier)
                Hub.Barrier.SignalAndWait(TimeSpan.FromSeconds(60), token);
            await Click(PLUS, 0_100, token).ConfigureAwait(false);

            // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
            // Confirming...
            for (int i = 0; i < 4; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            poke.TradeSearching(this);
            await Task.Delay(Util.Rand.Next(100, 1000), token).ConfigureAwait(false);

            await Click(A, 1_000, token).ConfigureAwait(false);

            // Clear the shown data offset.
            await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, ShownTradeDataOffset, token).ConfigureAwait(false);

            // Wait 40 Seconds for Trainer...
            var partnerFound = await ReadUntilChanged(ShownTradeDataOffset, PokeTradeBotUtil.EMPTY_EC, 40_000, 2_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;
            if (!partnerFound)
            {
                await ResetTradePosition(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Potential Trainer Found!

            // Select Pokemon
            // pkm already injected to b1s1
            var TrainerName = await GetTradePartnerName(token).ConfigureAwait(false);
            Connection.Log($"Found Trading Partner: {TrainerName} ...");
            await Task.Delay(300, token).ConfigureAwait(false);

            await Click(A, 2_000, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);

            // Wait for User Input...
            var pk = await ReadUntilPresent(ShownTradeDataOffset, 25_000, 1_000, token).ConfigureAwait(false);
            if (pk == null)
            {
                await ExitTrade(Overworld, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);

            // Wait 30 Seconds until Trade is finished...
            await Task.Delay(30_000 + Util.Rand.Next(500, 5000), token).ConfigureAwait(false);

            // Pokemon has probably arrived, bypass Trade Evolution...
            await Click(Y, 1_000, token).ConfigureAwait(false);

            await Task.Delay(1_000 + Util.Rand.Next(500, 5000), token).ConfigureAwait(false);

            await ExitTrade(Overworld, token).ConfigureAwait(false);

            for (int i = 0; i < 3; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);

            // Spam A Button in Case of Trade Evolution/Moves Learning/Dex
            for (int i = 0; i < 20; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);

            for (int i = 0; i < 3; i++)
                await Click(B, 1_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            if (await ReadBoxPokemon(0, 0, token).ConfigureAwait(false) == pkm)
            {
                Connection.Log("Trade failed, return to Overworld!");
                await ResetTradePosition(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }
            else
            {
                Connection.Log("Trade was successful.");
            }

            // Trade was Successful!
            poke.TradeFinished(this, pkm);
            Connection.Log("Trade complete!");
            Hub.AddCompletedTrade();
            if (Dump && !string.IsNullOrEmpty(DumpFolder))
                DumpPokemon(DumpFolder, await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false));

            await Task.Delay(5_000, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> PerformSurpriseTrade(SAV8SWSH sav, PK8 pkm, CancellationToken token)
        {
            await Task.Delay(5_000, token).ConfigureAwait(false);
            // Inject to b1s1
            Connection.Log("Starting next Surprise Trade. Getting data...");
            await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            Connection.Log("Open Y-COM Menu");
            await Click(Y, 1_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Select Surprise Trade");
            await Click(DDOWN, 0_100, token).ConfigureAwait(false);
            await Click(A, 4_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Select Pokemon");
            // Box 1 Slot 1
            await Click(A, 0_700, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Confirming...");
            await Click(A, 8_000, token).ConfigureAwait(false);
            for (int i = 0; i < 3; i++)
                await Click(A, 0_700, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            // Time we wait for a trade
            await Task.Delay(45_000, token).ConfigureAwait(false);
            await Click(Y, 0_700, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            await WaitForSurpriseTradeToFinish(token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Trade complete!");
            if (Dump && !string.IsNullOrEmpty(DumpFolder))
                DumpPokemon(DumpFolder, await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false));

            await Task.Delay(5_000, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> PerformDuduTrade(CancellationToken token)
        {
            await Task.Delay(5_000, token).ConfigureAwait(false);
            Connection.Log("Starting next Dudu Bot Trade. Getting data...");
            Connection.Log("Open Y-COM Menu");
            await Click(Y, 1_000, token).ConfigureAwait(false);

            Connection.Log("Select Link Trade");
            await Click(A, 1_000, token).ConfigureAwait(false);

            Connection.Log("Select Link Trade Code");
            await Click(DDOWN, 200, token).ConfigureAwait(false);

            for (int i = 0; i < 2; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            // Loading Screen
            await Task.Delay(2_000, token).ConfigureAwait(false);

            var code = Hub.Config.GetRandomTradeCode();
            Connection.Log($"Entering Link Trade Code: {code} ...");
            await EnterTradeCode(code, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            if (ShouldWaitAtBarrier && Hub.UseBarrier)
                Hub.Barrier.SignalAndWait(TimeSpan.FromSeconds(60), token);
            await Click(PLUS, 0_100, token).ConfigureAwait(false);

            // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
            // Confirming...
            for (int i = 0; i < 4; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            Connection.Log("Searching for a trade partner");
            await Task.Delay(Util.Rand.Next(100, 1000), token).ConfigureAwait(false);

            await Click(A, 1_000, token).ConfigureAwait(false);

            // Clear the shown data offset.
            await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, ShownTradeDataOffset, token).ConfigureAwait(false);

            // Wait 40 Seconds for Trainer...
            var partnerFound = await ReadUntilChanged(ShownTradeDataOffset, PokeTradeBotUtil.EMPTY_EC, 40_000, 2_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;
            if (!partnerFound)
            {
                await ResetTradePosition(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Potential Trainer Found!

            // Select Pokemon
            // pkm already injected to b1s1
            var TrainerName = await GetTradePartnerName(token).ConfigureAwait(false);
            Connection.Log($"Found Trading Partner: {TrainerName} ...");

            // Wait for User Input...
            var pk = await ReadUntilPresent(ShownTradeDataOffset, 25_000, 1_000, token).ConfigureAwait(false);
            if (pk == null)
            {
                await ExitTrade(Overworld, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            await ExitTrade(Overworld, token).ConfigureAwait(false);
            var ec = pk.EncryptionConstant;
            var pid = pk.PID;
            var IVs = pk.IVs.Length == 0 ? GetBlankIVTemplate() : PKX.ReorderSpeedLast((int[])pk.IVs.Clone());
            if (pk.IsShiny)
            {
                Connection.Log("The pokemon is already shiny!"); // Do not bother checking for next shiny frame
                return PokeTradeResult.Success;
            }

            var match = Z3Search.GetFirstSeed(ec, pid, IVs, out var seed);
            switch (match)
            {
                case Z3SearchResult.SeedNone:
                    Connection.Log("The pokemon is not a raid pokemon!");
                    break;
                case Z3SearchResult.SeedMismatch:
                    Connection.Log("No valid seed found!");
                    break;
                default:
                    Connection.Log($"Seed: {seed:X16}");
                    Connection.Log($"Next Shiny Frame: {Z3Search.GetNextShinyFrame(seed, out var type)}");
                    var shinytype = type == 1 ? "Star" : "Square";
                    Connection.Log($"Shiny Type: {shinytype}");
                    break;
            }

            await Task.Delay(5_000, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
        }

        private async Task WaitForSurpriseTradeToFinish(CancellationToken token)
        {
            // Spam A Button in Case of Trade Evolution/Moves Learning/Dex
            for (int i = 0; i < 20; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);
        }

        private void ExitLinkTradeRoutine()
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
