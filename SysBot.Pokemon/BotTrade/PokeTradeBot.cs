using System;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> Hub;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        private readonly IDumper DumpSetting;

        /// <summary>
        /// Synchronized start for multiple bots.
        /// </summary>
        public bool ShouldWaitAtBarrier { get; private set; }

        /// <summary>
        /// Tracks failed synchronized starts to attempt to re-sync.
        /// </summary>
        public int FailedBarrier { get; private set; }

        public PokeTradeBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Hub = hub;
            DumpSetting = hub.Config;
        }

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        protected override async Task MainLoop(CancellationToken token)
        {
            Connection.Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            Hub.Bots.Add(this);
            Connection.Log("Starting main TradeBot loop.");
            while (!token.IsCancellationRequested)
            {
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    PokeRoutineType.Idle => DoNothing(token),
                    PokeRoutineType.SurpriseTrade => DoSurpriseTrades(sav, token),
                    _ => DoTrades(sav, token),
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

        private async Task DoTrades(SAV8SWSH sav, CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType != PokeRoutineType.Idle)
            {
                var type = Config.CurrentRoutineType;
                if (!Hub.Queues.TryDequeue(Config.CurrentRoutineType, out var detail, out var priority))
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
                string tradetype = $" ({detail.Type.ToString()})";
                Connection.Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
                await EnsureConnectedToYComm(token).ConfigureAwait(false);
                var result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);
                if (result != PokeTradeResult.Success) // requeue
                {
                    if (result == PokeTradeResult.Aborted)
                    {
                        detail.SendNotification(this, "Oops! Something happened. I'll requeue you for another attempt.");
                        Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradeQueue<PK8>.Tier2));
                    }
                    else
                    {
                        detail.SendNotification(this, $"Oops! Something happened. Canceling the trade: {result}.");
                        detail.TradeCanceled(this, result);
                    }
                }
            }

            UpdateBarrier(false);
        }

        private async Task DoSurpriseTrades(SAV8SWSH sav, CancellationToken token)
        {
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.SurpriseTrade)
            {
                var pkm = Hub.Ledy.Pool.GetRandomSurprise();
                await EnsureConnectedToYComm(token).ConfigureAwait(false);
                var _ = await PerformSurpriseTrade(sav, pkm, token).ConfigureAwait(false);
            }
        }

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8SWSH sav, PokeTradeDetail<PK8> poke, CancellationToken token)
        {
            // Update Barrier Settings
            UpdateBarrier(poke.IsSynchronized);
            poke.TradeInitialize(this);

            var pkm = poke.TradeData;
            if (pkm.Species != 0)
                await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.Recover;
            }

            Connection.Log("Opening Y-Comm Menu");
            await Click(Y, 1_700, token).ConfigureAwait(false);

            Connection.Log("Selecting Link Trade");
            await Click(A, 1_500, token).ConfigureAwait(false);

            Connection.Log("Selecting Link Trade Code");
            await Click(DDOWN, 500, token).ConfigureAwait(false);

            for (int i = 0; i < 2; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);

            // These languages require an extra A press at this menu.
            if (GameLang == LanguageID.Korean || GameLang == LanguageID.German)
                await Click(A, 1_500, token).ConfigureAwait(false);

            // Loading Screen
            await Task.Delay(2_000, token).ConfigureAwait(false);

            var code = poke.Code;
            Connection.Log($"Entering Link Trade Code: {code:0000}...");
            await EnterTradeCode(code, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);
            await Click(PLUS, 1_000, token).ConfigureAwait(false);

            // Start a Link Trade, in case of Empty Slot/Egg/Bad Pokemon we press sometimes B to return to the Overworld and skip this Slot.
            // Confirming...
            for (int i = 0; i < 4; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);

            poke.TradeSearching(this);
            await Task.Delay(Util.Rand.Next(0_350, 0_750), token).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

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

            // Confirm Box 1 Slot 1
            if (poke.Type == PokeTradeType.Specific)
            {
                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }

            poke.SendNotification(this, $"Found Trading Partner: {TrainerName}. Waiting for a Pokémon...");

            if (poke.Type == PokeTradeType.Dump)
                return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

            // Wait for User Input...
            var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, token).ConfigureAwait(false);
            if (pk == null)
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            if (poke.Type == PokeTradeType.Dudu)
            {
                // Immediately exit, we aren't trading anything.
                return await EndDuduTradeAsync(poke, pk, token).ConfigureAwait(false);
            }

            if (poke.Type == PokeTradeType.Random) // distribution
            {
                // Allow the trade partner to do a Ledy swap.
                var trade = Hub.Ledy.GetLedyTrade(pk, Hub.Config.DistributeLedySpecies);
                pkm = trade.Receive;
                if (trade.Type != LedyResponseType.Random)
                {
                    poke.SendNotification(this, "Injecting your requested Pokémon.");
                    await Click(A, 0_800, token).ConfigureAwait(false);
                    await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);
                    await Task.Delay(2_500, token).ConfigureAwait(false);
                }
                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }
            else if (poke.Type == PokeTradeType.Clone)
            {
                // Inject the shown Pokemon.
                var clone = (PK8)pk.Clone();

                var la = new LegalityAnalysis(clone);
                if (!la.Valid)
                {
                    Connection.Log("Clone request has detected an invalid Pokémon.");
                    if (DumpSetting.Dump)
                        DumpPokemon(DumpSetting.DumpFolder, "hacked", clone);

                    var report = la.Report();
                    Connection.Log(report);
                    poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                    poke.SendNotification(this, report);

                    return PokeTradeResult.InvalidData;
                }

                if (Hub.Config.ResetHOMETracker)
                    clone.Tracker = 0;

                poke.SendNotification(this, $"**Cloned your {(Species)clone.Species}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
                Connection.Log($"Cloned a {(Species)clone.Species}. Waiting for user to change their Pokémon...");

                // Clear the shown data offset.
                await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, LinkTradePartnerPokemonOffset, token).ConfigureAwait(false);

                // Wait for User Input...
                var pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 10_000, 1_000, token).ConfigureAwait(false);
                if (pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(pk))
                {
                    poke.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");

                    // They get one more chance.
                    // Clear the shown data offset.
                    await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, LinkTradePartnerPokemonOffset, token).ConfigureAwait(false);

                    // Wait for User Input...
                    pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 5_000, 1_000, token).ConfigureAwait(false);
                    if (pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(pk))
                    {
                        Connection.Log("Trading partner did not change their Pokémon.");
                        await ExitTrade(true, token).ConfigureAwait(false);
                        return PokeTradeResult.TrainerTooSlow;
                    }
                }

                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(clone, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);
                pkm = clone;

                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
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
            // Pokemon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (SearchUtil.HashByDetails(traded) == SearchUtil.HashByDetails(pkm))
            {
                Connection.Log("User did not complete the trade.");
                return PokeTradeResult.TrainerTooSlow;
            }
            else
            {
                // As long as we got rid of our inject in b1s1, assume the trade went through.
                Connection.Log("User completed the trade.");
                poke.TradeFinished(this, traded);

                // Only log if we completed the trade.
                var counts = Hub.Counts;
                if (poke.Type == PokeTradeType.Random)
                    counts.AddCompletedDistribution();
                else if (poke.Type == PokeTradeType.Clone)
                    counts.AddCompletedClones();
                else
                    Hub.Counts.AddCompletedTrade();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                {
                    var subfolder = poke.Type.ToString().ToLower();
                    DumpPokemon(DumpSetting.DumpFolder, subfolder, traded); // received
                    if (poke.Type == PokeTradeType.Specific || poke.Type == PokeTradeType.Clone)
                        DumpPokemon(DumpSetting.DumpFolder, "traded", pkm); // sent to partner
                }
            }

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PK8> poke, CancellationToken token)
        {
            await Task.Delay(1, token).ConfigureAwait(false);
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

            Connection.Log("Opening Y-Comm Menu");
            await Click(Y, 2_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Connection.Log("Selecting Surprise Trade");
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

            Connection.Log("Selecting Pokémon");
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

            Connection.Log($"Found Surprise Trade Partner: {TrainerName}, Pokemon: {(Species)SuprisePoke.Species}");

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

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "surprise", SuprisePoke);
            Hub.Counts.AddCompletedSurprise();

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> EndDuduTradeAsync(PokeTradeDetail<PK8> detail, PK8 pk, CancellationToken token)
        {
            await ExitDuduTrade(token).ConfigureAwait(false);

            detail.TradeFinished(this, pk);

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "seed", pk);

            // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
#pragma warning disable 4014
            Task.Run(() =>
            {
                try
                {
                    ReplyWithZ3Results(detail, pk);
                }
                catch (Exception ex)
                {
                    detail.SendNotification(this, $"Unable to calculate seeds: {ex.Message}\r\n{ex.StackTrace}");
                }
            }, token);
#pragma warning restore 4014

            Hub.Counts.AddCompletedDudu();

            await Task.Delay(5_000, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
        }

        private void ReplyWithZ3Results(PokeTradeDetail<PK8> detail, PK8 result)
        {
            detail.SendNotification(this, "Calculating your seed(s)...");

            if (result.IsShiny)
            {
                Connection.Log("The Pokémon is already shiny!"); // Do not bother checking for next shiny frame
                detail.SendNotification(this, "This Pokémon is already shiny! Raid seed calculation was not done.");

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "seed", result);

                detail.TradeFinished(this, result);
                return;
            }

            var ec = result.EncryptionConstant;
            var pid = result.PID;
            var IVs = result.IVs.Length == 0 ? GetBlankIVTemplate() : PKX.ReorderSpeedLast((int[])result.IVs.Clone());
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
                var lump = new PokeTradeSummary("Calculated Seed:", match);
                detail.SendNotification(this, lump);
            }
            Connection.Log("Seed calculation completed.");
        }

        private void WaitAtBarrierIfApplicable(CancellationToken token)
        {
            if (!ShouldWaitAtBarrier)
                return;
            var opt = Hub.Config.SynchronizeBots;
            if (opt == BotSyncOption.NoSync)
                return;

            var timeoutAfter = Hub.Config.SynchronizeTimeout;
            if (FailedBarrier == 1) // failed last iteration
                timeoutAfter *= 2; // try to re-sync in the event things are too slow.

            var result = opt == BotSyncOption.LocalSync
                ? Hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token)
                : Hub.BotSync.RemoteBarrier.WaitOne(TimeSpan.FromSeconds(timeoutAfter));

            if (result)
            {
                FailedBarrier = 0;
                return;
            }

            FailedBarrier++;
            Connection.Log($"Barrier sync timed out after {timeoutAfter} seconds. Continuing.");
        }

        /// <summary>
        /// Checks if the barrier needs to get updated to consider this bot.
        /// If it should be considered, it adds it to the barrier if it is not already added.
        /// If it should not be considered, it removes it from the barrier if not already removed.
        /// </summary>
        private void UpdateBarrier(bool shouldWait)
        {
            if (ShouldWaitAtBarrier == shouldWait)
                return; // no change required

            ShouldWaitAtBarrier = shouldWait;
            if (shouldWait)
            {
                Hub.BotSync.Barrier.AddParticipant();
                Connection.Log($"Joined the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
            }
            else
            {
                Hub.BotSync.Barrier.RemoveParticipant();
                Connection.Log($"Left the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
            }
        }
    }
}
