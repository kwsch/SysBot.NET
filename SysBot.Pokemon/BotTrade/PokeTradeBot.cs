using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.Searching;
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
            await EchoCommands(false, token).ConfigureAwait(false);
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
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (waitCounter == 0)
                    Connection.Log("No task assigned. Waiting for new task assignment.");
                waitCounter++;
                if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                    await Click(B, 1_000, token).ConfigureAwait(false);
                else
                    await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task DoLinkTrades(SAV8SWSH sav, CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.LinkTrade)
            {
                if (!Hub.Queue.TryDequeue(out var poke, out var priority))
                {
                    if (waitCounter == 0)
                        Connection.Log("Nothing to send in the queue! Waiting for new trade data.");
                    waitCounter++;
                    if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                        await Click(B, 1_000, token).ConfigureAwait(false);
                    else
                        await Task.Delay(1_000, token).ConfigureAwait(false);
                    continue;
                }

                waitCounter = 0;
                Connection.Log("Starting next Link Code trade. Getting data...");
                await EnsureConnectedToYCom(token).ConfigureAwait(false);
                var result = await PerformLinkCodeTrade(sav, poke, token).ConfigureAwait(false);
                if (result != PokeTradeResult.Success) // requeue
                {
                    if (result == PokeTradeResult.Aborted)
                    {
                        poke.SendNotification(this, "Oops! Something happened. I'll requeue you for another attempt.");
                        Hub.Queue.Enqueue(poke, Math.Min(priority, PokeTradeQueue<PK8>.Tier2));
                    }
                    else
                    {
                        poke.SendNotification(this, $"Oops! Something happened. Canceling the trade: {result}.");
                        poke.TradeCanceled(this, result);
                    }
                }
            }

            ExitLinkTradeRoutine();
        }

        private async Task DoDuduTrades(CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.DuduBot)
            {
                if (!Hub.Dudu.TryDequeue(out var detail, out var priority))
                {
                    if (waitCounter == 0)
                        Connection.Log("Nothing to check, waiting for new users...");
                    waitCounter++;
                    if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                        await Click(B, 1_000, token).ConfigureAwait(false);
                    else
                        await Task.Delay(1_000, token).ConfigureAwait(false);
                    continue;
                }

                waitCounter = 0;
                Connection.Log("Starting next Dudu Bot Trade. Getting data...");
                await EnsureConnectedToYCom(token).ConfigureAwait(false);
                var result = await PerformDuduTrade(detail, token).ConfigureAwait(false);
                if (result != PokeTradeResult.Success) // requeue
                {
                    if (result == PokeTradeResult.Aborted)
                    {
                        detail.SendNotification(this, "Oops! Something happened. I'll requeue you for another attempt.");
                        Hub.Dudu.Enqueue(detail, Math.Min(priority, PokeTradeQueue<PK8>.Tier2));
                    }
                    else
                    {
                        detail.SendNotification(this, $"Oops! Something happened. Canceling the trade: {result}.");
                        detail.TradeCanceled(this, result);
                    }
                }
            }
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

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8SWSH sav, PokeTradeDetail<PK8> poke, CancellationToken token)
        {
            poke.TradeInitialize(this);
            // Update Barrier Settings
            ShouldWaitAtBarrier = UpdateBarrier(Hub.Barrier, poke.IsRandomCode, ShouldWaitAtBarrier);
            var pkm = poke.TradeData;
            await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            Connection.Log("Open Y-Comm Menu");
            await Click(Y, 2_000, token).ConfigureAwait(false);

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
            if (ShouldWaitAtBarrier && Hub.Config.SynchronizeLinkTradeBots)
                Hub.Barrier.SignalAndWait(TimeSpan.FromSeconds(Hub.Config.SynchronizeLinkTradeBotsTimeout), token);
            await Click(PLUS, 1_000, token).ConfigureAwait(false);

            // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
            // Confirming...
            for (int i = 0; i < 4; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            poke.TradeSearching(this);
            await Task.Delay(Util.Rand.Next(0_350, 0_750), token).ConfigureAwait(false);

            await Click(A, 2_000, token).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            // Clear the shown data offset.
            await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, LinkTradePartnerPokemonOffset, token).ConfigureAwait(false);

            // Wait 40 Seconds for Trainer...
            var partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, PokeTradeBotUtil.EMPTY_EC, 90_000, 0_200, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;
            if (!partnerFound)
            {
                await ResetTradePosition(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Select Pokemon
            // pkm already injected to b1s1
            var TrainerName = await GetTradePartnerName(TradeMethod.LinkTrade, token).ConfigureAwait(false);
            Connection.Log($"Found Trading Partner: {TrainerName} ...");
            await Task.Delay(1_500, token).ConfigureAwait(false); // necessary delay to get to the box properly

            if (!await IsCorrectScreen(CurrentScreen_Box, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            for(int i = 0; i < 5; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);
            
            poke.SendNotification(this, $"Found Trading Partner: {TrainerName}. Waiting for a Pokemon ...");

            // Wait for User Input...
            var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, token).ConfigureAwait(false);
            if (pk == null)
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);

            var delay_count = 0;
            while (!await IsCorrectScreen(CurrentScreen_Box, token).ConfigureAwait(false))
            {
                await Click(A, 3_000, token).ConfigureAwait(false);
                delay_count++;
                if (delay_count >= 50)
                    break;
                if (await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false)) // In case we are in a Trade Evolution/PokeDex Entry and the Trade Partner quits we land on the Overworld
                    break;
            }

            await Task.Delay(1_000 + Util.Rand.Next(0_700, 1_000), token).ConfigureAwait(false);

            await ExitTrade(false, token).ConfigureAwait(false);
            Connection.Log("Exited Trade!");

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            // Trade was Successful!
            var traded = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (SearchUtil.HashByDetails(traded) == SearchUtil.HashByDetails(pk))
                Connection.Log("User traded the initially shown file.");
            else if (SearchUtil.HashByDetails(traded) != SearchUtil.HashByDetails(pkm))
                Connection.Log("User did not complete the trade.");
            else
                Connection.Log("Recipient changed the traded Pokémon after initially showing another.");

            poke.TradeFinished(this, traded);
            Connection.Log("Trade complete!");
            Hub.AddCompletedTrade();
            if (Dump && !string.IsNullOrEmpty(DumpFolder))
            {
                DumpPokemon(DumpFolder, traded);
            }

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> PerformSurpriseTrade(SAV8SWSH sav, PK8 pkm, CancellationToken token)
        {
            // General Bot Strategy:
            // 1. Inject to b1s1
            // 2. Send out Trade
            // 3. Clear received PKM to skip the trade animation
            // 4. Repeat

            // Inject to b1s1

            Connection.Log("Starting next Surprise Trade. Getting data...");
            await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            Connection.Log("Open Y-Comm Menu");
            await Click(Y, 2_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Select Surprise Trade");
            await Click(DDOWN, 0_500, token).ConfigureAwait(false);
            await Click(A, 4_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            await Task.Delay(2_000, token).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Box, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            Connection.Log("Select Pokemon");
            // Box 1 Slot 1; no movement required.
            await Click(A, 0_700, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Confirming...");
            await Click(A, 5_000, token).ConfigureAwait(false);
            for (int i = 0; i < 3; i++)
                await Click(A, 0_700, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            // Let Surprise Trade be sent out before checking if we're back to the Overworld.
            await Task.Delay(3_000, token).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            Connection.Log("Waiting for Surprise Trade Partner...");

            // Time we wait for a trade
            var partnerFound = await ReadUntilChanged(SupriseTradePartnerPokemonOffset, PokeTradeBotUtil.EMPTY_SLOT, 90_000, 50, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            if (!partnerFound)
            {
                await ResetTradePosition(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Let the game flush the results and de-register from the online surprise trade queue.
            await Task.Delay(7_000, token).ConfigureAwait(false);

            var TrainerName = await GetTradePartnerName(TradeMethod.SupriseTrade, token).ConfigureAwait(false);
            var SuprisePoke = await ReadSupriseTradePokemon(token).ConfigureAwait(false);

            Connection.Log($"Found Surprise Trade Partner: {TrainerName} , Pokemon: {(Species)SuprisePoke.Species}");

            // Clear out the received trade data; we want to skip the trade animation.
            // The box slot locks have been removed prior to searching.

            await Connection.WriteBytesAsync(BitConverter.GetBytes(SupriseTradeSearch_Empty), SupriseTradeSearchOffset, token).ConfigureAwait(false);
            await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, SupriseTradePartnerPokemonOffset, token).ConfigureAwait(false);

            // Let the game recognize our modifications before finishing this loop.
            await Task.Delay(5_000, token).ConfigureAwait(false);

            // Clear the Surprise Trade slot locks! We'll skip the trade animation and reuse the slot on later loops.
            // Write 8 bytes of FF to set both Int32's to -1. Regular locks are [Box32][Slot32]

            await Connection.WriteBytesAsync(BitConverter.GetBytes(ulong.MaxValue), SupriseTradeLockBox, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            if (await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                Connection.Log("Trade complete!");
            else
                await ExitTrade(true, token).ConfigureAwait(false);

            if (Dump && !string.IsNullOrEmpty(DumpFolder))
                DumpPokemon(DumpFolder, SuprisePoke);

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> PerformDuduTrade(PokeTradeDetail<PK8> detail, CancellationToken token)
        {
            detail.TradeInitialize(this);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            Connection.Log("Open Y-Comm Menu");
            await Click(Y, 2_000, token).ConfigureAwait(false);

            Connection.Log("Select Link Trade");
            await Click(A, 1_000, token).ConfigureAwait(false);

            Connection.Log("Select Link Trade Code");
            await Click(DDOWN, 200, token).ConfigureAwait(false);

            for (int i = 0; i < 2; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            // Loading Screen
            await Task.Delay(2_000, token).ConfigureAwait(false);

            var code = detail.Code;
            if (code < 0)
                code = Hub.Config.GetRandomTradeCode();
            Connection.Log($"Entering Link Trade Code: {code} ...");
            await EnterTradeCode(code, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            if (ShouldWaitAtBarrier && Hub.Config.SynchronizeLinkTradeBots)
                Hub.Barrier.SignalAndWait(TimeSpan.FromSeconds(60), token);
            await Click(PLUS, 0_100, token).ConfigureAwait(false);

            // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
            // Confirming...
            for (int i = 0; i < 4; i++)
                await Click(A, 2_000, token).ConfigureAwait(false);

            detail.TradeSearching(this);
            await Task.Delay(Util.Rand.Next(100, 1000), token).ConfigureAwait(false);

            await Click(A, 2_000, token).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            // Clear the shown data offset.
            await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, LinkTradePartnerPokemonOffset, token).ConfigureAwait(false);

            // Wait 40 Seconds for Trainer...
            var partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, PokeTradeBotUtil.EMPTY_EC, 90_000, 2_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;
            if (!partnerFound)
            {
                await ResetTradePosition(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Potential Trainer Found!

            // Select Pokemon
            var TrainerName = await GetTradePartnerName(TradeMethod.LinkTrade, token).ConfigureAwait(false);
            Connection.Log($"Found Trading Partner: {TrainerName} ...");
            await Task.Delay(1_000, token).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Box, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            detail.SendNotification(this, $"Found Trading Partner: {TrainerName}. Waiting for a Pokemon ...");

            // Wait for User Input...
            var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, token).ConfigureAwait(false);
            if (pk == null)
            {
                await ExitDuduTrade(token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            await ExitDuduTrade(token).ConfigureAwait(false);
            var ec = pk.EncryptionConstant;
            var pid = pk.PID;
            var IVs = pk.IVs.Length == 0 ? GetBlankIVTemplate() : PKX.ReorderSpeedLast((int[])pk.IVs.Clone());
            if (pk.IsShiny)
            {
                Connection.Log("The Pokemon is already shiny!"); // Do not bother checking for next shiny frame
                detail.SendNotification(this, "This Pokemon is already shiny! Raid seed calculation was not done.");
                detail.TradeFinished(this, pk);
                return PokeTradeResult.Success;
            }

            if (Hub.Config.ShowAllZ3Results)
            {
                var matches = Z3Search.GetAllSeeds(ec, pid, IVs);
                foreach (var match in matches)
                {
                    var msg = match.ToString();
                    detail.SendNotification(this, msg);
                }
            }
            else
            {
                var match = Z3Search.GetFirstSeed(ec, pid, IVs);
                var msg = match.ToString();
                detail.SendNotification(this, msg);
            }

            detail.TradeFinished(this, pk);

            await Task.Delay(5_000, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
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
