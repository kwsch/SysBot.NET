using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class PokeTradeBot : PokeRoutineExecutor8, ICountBot
    {
        public static ISeedSearchHandler<PK8> SeedChecker = new NoSeedSearchHandler<PK8>();
        private readonly PokeTradeHub<PK8> Hub;
        private readonly TradeSettings Settings;
        public ICountSettings Counts => Settings;

        private static readonly TrackedUserLog PreviousUsers = new();

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

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public PokeTradeBot(PokeTradeHub<PK8> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
            Settings = hub.Config.Trade;
            DumpSetting = hub.Config.Folder;
        }

        public override async Task MainLoop(CancellationToken token)
        {
            try
            {
                await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

                Log("Identifying trainer data of the host console.");
                var sav = await IdentifyTrainer(token).ConfigureAwait(false);

                Log($"Starting main {nameof(PokeTradeBot)} loop.");
                await InnerLoop(sav, token).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Log(e.Message);
            }

            Log($"Ending {nameof(PokeTradeBot)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            UpdateBarrier(false);
            await CleanExit(Settings, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task InnerLoop(SAV8SWSH sav, CancellationToken token)
        {
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
        }

        private async Task DoNothing(CancellationToken token)
        {
            int waitCounter = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            {
                if (waitCounter == 0)
                    Log("No task assigned. Waiting for new task assignment.");
                waitCounter++;
                if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                    await Click(B, 1_000, token).ConfigureAwait(false);
                else
                    await Task.Delay(1_000, token).ConfigureAwait(false);
            }
        }

        private async Task DoTrades(SAV8SWSH sav, CancellationToken token)
        {
            var type = Config.CurrentRoutineType;
            int waitCounter = 0;
            await SetCurrentBox(0, token).ConfigureAwait(false);
            while (!token.IsCancellationRequested && Config.NextRoutineType == type)
            {
                var (detail, priority) = GetTradeData(type);
                if (detail is null)
                {
                    await WaitForQueueStep(waitCounter++, token).ConfigureAwait(false);
                    continue;
                }
                waitCounter = 0;

                string tradetype = $" ({detail.Type})";
                Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
                Hub.Config.Stream.StartTrade(this, detail, Hub);
                Hub.Queues.StartTrade(this, detail);

                await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);
            }
        }

        private async Task WaitForQueueStep(int waitCounter, CancellationToken token)
        {
            if (waitCounter == 0)
            {
                // Updates the assets.
                Hub.Config.Stream.IdleAssets(this);
                Log("Nothing to check, waiting for new users...");
            }

            const int interval = 10;
            if (waitCounter % interval == interval-1 && Hub.Config.AntiIdle)
                await Click(B, 1_000, token).ConfigureAwait(false);
            else
                await Task.Delay(1_000, token).ConfigureAwait(false);
        }

        protected virtual (PokeTradeDetail<PK8>? detail, uint priority) GetTradeData(PokeRoutineType type)
        {
            if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
                return (detail, priority);
            if (Hub.Queues.TryDequeueLedy(out detail))
                return (detail, PokeTradePriorities.TierFree);
            return (null, PokeTradePriorities.TierFree);
        }

        private async Task PerformTrade(SAV8SWSH sav, PokeTradeDetail<PK8> detail, PokeRoutineType type, uint priority, CancellationToken token)
        {
            PokeTradeResult result;
            try
            {
                result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);
                if (result == PokeTradeResult.Success)
                    return;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Log(e.Message);
                result = PokeTradeResult.Aborted;
            }

            HandleAbortedTrade(detail, type, priority, result);
        }

        private void HandleAbortedTrade(PokeTradeDetail<PK8> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
        {
            if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
            {
                detail.IsRetry = true;
                Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
                detail.SendNotification(this, "Oops! Something happened. I'll requeue you for another attempt.");
            }
            else
            {
                detail.SendNotification(this, $"Oops! Something happened. Canceling the trade: {result}.");
                detail.TradeCanceled(this, result);
            }
        }

        private async Task DoSurpriseTrades(SAV8SWSH sav, CancellationToken token)
        {
            await SetCurrentBox(0, token).ConfigureAwait(false);
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.SurpriseTrade)
            {
                var pkm = Hub.Ledy.Pool.GetRandomSurprise();
                await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);
                var _ = await PerformSurpriseTrade(sav, pkm, token).ConfigureAwait(false);
            }
        }

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8SWSH sav, PokeTradeDetail<PK8> poke, CancellationToken token)
        {
            // Update Barrier Settings
            UpdateBarrier(poke.IsSynchronized);
            poke.TradeInitialize(this);
            await EnsureConnectedToYComm(Hub.Config, token).ConfigureAwait(false);
            Hub.Config.Stream.EndEnterCode(this);

            if (await CheckIfSoftBanned(token).ConfigureAwait(false))
                await Unban(token).ConfigureAwait(false);

            var toSend = poke.TradeData;
            if (toSend.Species != 0)
                await SetBoxPokemon(toSend, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            if (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverStart;
            }

            while (await CheckIfSearchingForLinkTradePartner(token).ConfigureAwait(false))
            {
                Log("Still searching, reset bot position.");
                await ResetTradePosition(Hub.Config, token).ConfigureAwait(false);
            }

            Log("Opening Y-Comm Menu");
            await Click(Y, 2_000, token).ConfigureAwait(false);

            Log("Selecting Link Trade");
            await Click(A, 1_500, token).ConfigureAwait(false);

            Log("Selecting Link Trade Code");
            await Click(DDOWN, 500, token).ConfigureAwait(false);

            for (int i = 0; i < 2; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);

            // All other languages require an extra A press at this menu.
            if (GameLang != LanguageID.English && GameLang != LanguageID.Spanish)
                await Click(A, 1_500, token).ConfigureAwait(false);

            // Loading Screen
            if (poke.Type != PokeTradeType.Random)
                Hub.Config.Stream.StartEnterCode(this);
            await Task.Delay(Hub.Config.Timings.ExtraTimeOpenCodeEntry, token).ConfigureAwait(false);

            var code = poke.Code;
            Log($"Entering Link Trade Code: {code:0000 0000}...");
            await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);
            await Click(PLUS, 1_000, token).ConfigureAwait(false);

            Hub.Config.Stream.EndEnterCode(this);

            // Confirming and return to overworld.
            var delay_count = 0;
            while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
            {
                if (delay_count >= 5)
                {
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                    return PokeTradeResult.RecoverPostLinkCode;
                }

                for (int i = 0; i < 5; i++)
                    await Click(A, 0_800, token).ConfigureAwait(false);
                delay_count++;
            }

            poke.TradeSearching(this);
            await Task.Delay(0_500, token).ConfigureAwait(false);

            // Wait for a Trainer...
            var partnerFound = await WaitForTradePartnerOffer(token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;
            if (!partnerFound)
            {
                await ResetTradePosition(Hub.Config, token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Select Pokemon
            // pkm already injected to b1s1
            await Task.Delay(5_500, token).ConfigureAwait(false); // necessary delay to get to the box properly

            var TrainerName = await GetTradePartnerName(TradeMethod.LinkTrade, token).ConfigureAwait(false);
            var TrainerNID = await GetTradePartnerNID(token).ConfigureAwait(false);
            RecordUtil<PokeTradeBot>.Record($"Initiating\t{TrainerNID:X16}\t{TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
            Log($"Found Trading Partner: {TrainerName} ({TrainerNID})");

            var partnerCheck = await CheckPartnerReputation(poke, TrainerNID, TrainerName, token).ConfigureAwait(false);
            if (partnerCheck != PokeTradeResult.Success)
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return partnerCheck;
            }

            if (!await IsInBox(token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverOpenBox;
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
            var offered = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
            var oldEC = await Connection.ReadBytesAsync(LinkTradePartnerPokemonOffset, 4, token).ConfigureAwait(false);
            if (offered is null)
            {
                if (poke.Type == PokeTradeType.Seed)
                    await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);
                else
                    await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            if (poke.Type == PokeTradeType.Seed)
            {
                // Immediately exit, we aren't trading anything.
                return await EndSeedCheckTradeAsync(poke, offered, token).ConfigureAwait(false);
            }

            PokeTradeResult update;
            (toSend, update) = await GetEntityToSend(sav, poke, offered, oldEC, toSend, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
                return update;

            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult != PokeTradeResult.Success)
                return tradeResult;

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            // Trade was Successful!
            var received = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend))
            {
                Log("User did not complete the trade.");
                RecordUtil<PokeTradeBot>.Record($"Cancelled\t{TrainerNID:X16}\t{TrainerName}\t{poke.Trainer.TrainerName}\\t{poke.ID}\t{toSend.EncryptionConstant:X8}\t{offered.EncryptionConstant:X8}");
                return PokeTradeResult.TrainerTooSlow;
            }

            // As long as we got rid of our inject in b1s1, assume the trade went through.
            Log("User completed the trade.");
            poke.TradeFinished(this, received);

            RecordUtil<PokeTradeBot>.Record($"Finished\t{TrainerNID:X16}\t{toSend.EncryptionConstant:X8}\t{received.EncryptionConstant:X8}");

            // Only log if we completed the trade.
            UpdateCountsAndExport(poke, received, toSend);
            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> CheckPartnerReputation(PokeTradeDetail<PK8> poke, ulong TrainerNID, string TrainerName, CancellationToken token)
        {
            var cooldown = PreviousUsers.TryGePrevious(TrainerNID);
            if (cooldown != null)
            {
                var delta = DateTime.Now - cooldown.Time;
                var user = poke.Trainer;
                Log($"Last saw {user.TrainerName} {delta.TotalMinutes:D1} minutes ago.");

                var cd = Settings.TradeCooldown;
                if (cd != 0 && TimeSpan.FromMinutes(cd) > delta)
                {
                    poke.Notifier.SendNotification(this, poke, "You have ignored the trade cooldown set by the bot owner. The owner has been notified.");
                    var msg = $"{user.TrainerName} ({user.ID}) was encountered {delta.TotalMinutes:D1} minutes ago ignoring the trade cooldown of {cd} minutes.";
                    if (Settings.EchoNintendoOnlineIDCooldown)
                        EchoUtil.Echo($"ID: {TrainerNID}");
                    EchoUtil.Echo(msg);
                }
            }

            var evade = PreviousUsers.TryRegister(TrainerNID, TrainerName, poke.Trainer.ID);
            if (evade != null)
            {
                var delta = DateTime.Now - evade.Time;
                var user = poke.Trainer;
                var msg = $"{user.TrainerName} ({user.ID}) was encountered {delta.TotalMinutes:D1} minutes ago using account {evade.Name} ({evade.RemoteID}).";
                EchoUtil.Echo(msg);
                if (Settings.EchoNintendoOnlineIDMulti)
                    EchoUtil.Echo($"ID: {TrainerNID}");

                if (delta > TimeSpan.FromMinutes(Settings.TradeAbuseExpiration) && Settings.TradeAbuseAction != TradeAbuseAction.Ignore)
                {
                    if (Settings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                        await BlockUser(token).ConfigureAwait(false);
                    return PokeTradeResult.SuspiciousActivity;
                }
            }

            var entry = Settings.BannedIDs.List.Find(z => z.ID == TrainerNID);
            if (entry != null)
            {
                var user = poke.Trainer;
                var msg = $"{user.TrainerName} ({user.ID}) is banned, and was encountered in-game using {TrainerName}.";
                if (!string.IsNullOrWhiteSpace(entry.Comment))
                    msg += $" Was banned for {entry.Comment}";
                EchoUtil.Echo(msg);
                return PokeTradeResult.SuspiciousActivity;
            }

            return PokeTradeResult.Success;
        }

        protected virtual async Task<bool> WaitForTradePartnerOffer(CancellationToken token)
        {
            Log("Waiting for trainer...");
            return await WaitForPokemonChanged(LinkTradePartnerPokemonOffset, Hub.Config.Trade.TradeWaitTime * 1_000, 0_200, token).ConfigureAwait(false);
        }

        private void UpdateCountsAndExport(PokeTradeDetail<PK8> poke, PK8 received, PK8 toSend)
        {
            var counts = Settings;
            if (poke.Type == PokeTradeType.Random)
                counts.AddCompletedDistribution();
            else if (poke.Type == PokeTradeType.Clone)
                counts.AddCompletedClones();
            else
                counts.AddCompletedTrade();

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
            {
                var subfolder = poke.Type.ToString().ToLower();
                DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // received by bot
                if (poke.Type is PokeTradeType.Specific or PokeTradeType.Clone)
                    DumpPokemon(DumpSetting.DumpFolder, "traded", toSend); // sent to partner
            }
        }

        private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PK8> detail, CancellationToken token)
        {
            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
            {
                if (await IsUserBeingShifty(detail, token).ConfigureAwait(false))
                    return PokeTradeResult.SuspiciousActivity;
                await Click(A, 1_500, token).ConfigureAwait(false);
            }

            var delay_count = 0;
            while (!await IsInBox(token).ConfigureAwait(false))
            {
                await Click(A, 1_000, token).ConfigureAwait(false);
                delay_count++;
                if (delay_count >= Hub.Config.Trade.TradeAnimationMaxDelaySeconds)
                    break;
                if (await IsOnOverworld(Hub.Config, token).ConfigureAwait(false)) // In case we are in a Trade Evolution/PokeDex Entry and the Trade Partner quits we land on the Overworld
                    break;
            }

            await Task.Delay(1_000 + Util.Rand.Next(0_700, 1_000), token).ConfigureAwait(false);

            await ExitTrade(Hub.Config, false, token).ConfigureAwait(false);
            Log("Exited Trade!");
            return PokeTradeResult.Success;
        }

        protected virtual async Task<(PK8 toSend, PokeTradeResult check)> GetEntityToSend(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, byte[] oldEC, PK8 toSend, CancellationToken token)
        {
            return poke.Type switch
            {
                PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, token).ConfigureAwait(false),
                PokeTradeType.Clone => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
                _ => (toSend, PokeTradeResult.Success),
            };
        }

        private async Task<(PK8 toSend, PokeTradeResult check)> HandleClone(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, byte[] oldEC, CancellationToken token)
        {
            if (Hub.Config.Discord.ReturnPKMs)
                poke.SendNotification(this, offered, "Here's what you showed me!");

            var la = new LegalityAnalysis(offered);
            if (!la.Valid)
            {
                Log($"Clone request (from {poke.Trainer.TrainerName}) has detected an invalid Pokémon: {(Species)offered.Species}.");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

                var report = la.Report();
                Log(report);
                poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                poke.SendNotification(this, report);

                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                {
                    return (offered, PokeTradeResult.IllegalTrade);
                }
            }

            // Inject the shown Pokémon.
            var clone = (PK8)offered.Clone();
            if (Hub.Config.Legality.ResetHOMETracker)
                clone.Tracker = 0;

            poke.SendNotification(this, $"**Cloned your {(Species)clone.Species}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
            Log($"Cloned a {(Species)clone.Species}. Waiting for user to change their Pokémon...");

            // Separate this out from WaitForPokemonChanged since we compare to old EC from original read.
            var partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, oldEC, 15_000, 0_200, false, token)
                .ConfigureAwait(false);

            if (!partnerFound)
            {
                poke.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");
                // They get one more chance.
                partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, oldEC, 15_000, 0_200, false, token).ConfigureAwait(false);
            }

            var pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (!partnerFound || pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
            {
                Log("Trading partner did not change their Pokémon.");
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return (offered, PokeTradeResult.TrainerTooSlow);
            }

            await Click(A, 0_800, token).ConfigureAwait(false);
            await SetBoxPokemon(clone, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);

            return (clone, PokeTradeResult.Success);
        }

        private async Task<(PK8 toSend, PokeTradeResult check)> HandleRandomLedy(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, PK8 toSend, CancellationToken token)
        {
            // Allow the trade partner to do a Ledy swap.
            var trade = Hub.Ledy.GetLedyTrade(offered, Hub.Config.Distribution.LedySpecies);
            if (trade != null)
            {
                toSend = trade.Receive;
                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(toSend, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);
                await Task.Delay(2_500, token).ConfigureAwait(false);
            }

            for (int i = 0; i < 5; i++)
            {
                if (await IsUserBeingShifty(poke, token).ConfigureAwait(false))
                    return (toSend, PokeTradeResult.SuspiciousActivity);
                await Click(A, 0_500, token).ConfigureAwait(false);
            }

            return (toSend, PokeTradeResult.Success);
        }

        protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PK8> detail, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return false;
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PK8> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;
            var pkprev = new PK8();
            while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
            {
                var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
                if (pk == null || pk.Species < 1 || !pk.ChecksumValid || SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
                    continue;

                // Save the new Pokémon for comparison next round.
                pkprev = pk;

                // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
                if (DumpSetting.Dump)
                {
                    var subfolder = detail.Type.ToString().ToLower();
                    DumpPokemon(DumpSetting.DumpFolder, subfolder, pk); // received
                }

                var la = new LegalityAnalysis(pk);
                var verbose = la.Report(true);
                Log($"Shown Pokémon is {(la.Valid ? "Valid" : "Invalid")}.");

                detail.SendNotification(this, pk, verbose);
                ctr++;
            }

            Log($"Ended Dump loop after processing {ctr} Pokémon");
            await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);
            if (ctr == 0)
                return PokeTradeResult.TrainerTooSlow;

            Settings.AddCompletedDumps();
            detail.Notifier.SendNotification(this, detail, $"Dumped {ctr} Pokémon.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
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
            if (await CheckIfSoftBanned(token).ConfigureAwait(false))
                await Unban(token).ConfigureAwait(false);

            Log("Starting next Surprise Trade. Getting data...");
            await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

            if (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverStart;
            }

            if (await CheckIfSearchingForSurprisePartner(token).ConfigureAwait(false))
            {
                Log("Still searching, reset.");
                await ResetTradePosition(Hub.Config, token).ConfigureAwait(false);
            }

            Log("Opening Y-Comm Menu");
            await Click(Y, 1_500, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Log("Selecting Surprise Trade");
            await Click(DDOWN, 0_500, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            await Task.Delay(0_750, token).ConfigureAwait(false);

            if (!await IsInBox(token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverPostLinkCode;
            }

            Log("Selecting Pokémon");
            // Box 1 Slot 1; no movement required.
            await Click(A, 0_700, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            Log("Confirming...");
            while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                await Click(A, 0_800, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            // Let Surprise Trade be sent out before checking if we're back to the Overworld.
            await Task.Delay(3_000, token).ConfigureAwait(false);

            if (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
            {
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverReturnOverworld;
            }

            // Wait 30 Seconds for Trainer...
            Log("Waiting for Surprise Trade Partner...");

            // Wait for an offer...
            var oldEC = await Connection.ReadBytesAsync(SurpriseTradeSearchOffset, 4, token).ConfigureAwait(false);
            var partnerFound = await ReadUntilChanged(SurpriseTradeSearchOffset, oldEC, Hub.Config.Trade.TradeWaitTime * 1_000, 0_200, false, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            if (!partnerFound)
            {
                await ResetTradePosition(Hub.Config, token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            // Let the game flush the results and de-register from the online surprise trade queue.
            await Task.Delay(7_000, token).ConfigureAwait(false);

            var TrainerName = await GetTradePartnerName(TradeMethod.SupriseTrade, token).ConfigureAwait(false);
            var SurprisePoke = await ReadSurpriseTradePokemon(token).ConfigureAwait(false);

            Log($"Found Surprise Trade Partner: {TrainerName}, Pokémon: {(Species)SurprisePoke.Species}");

            // Clear out the received trade data; we want to skip the trade animation.
            // The box slot locks have been removed prior to searching.

            await Connection.WriteBytesAsync(BitConverter.GetBytes(SurpriseTradeSearch_Empty), SurpriseTradeSearchOffset, token).ConfigureAwait(false);
            await Connection.WriteBytesAsync(PokeTradeBotUtil.EMPTY_SLOT, SurpriseTradePartnerPokemonOffset, token).ConfigureAwait(false);

            // Let the game recognize our modifications before finishing this loop.
            await Task.Delay(5_000, token).ConfigureAwait(false);

            // Clear the Surprise Trade slot locks! We'll skip the trade animation and reuse the slot on later loops.
            // Write 8 bytes of FF to set both Int32's to -1. Regular locks are [Box32][Slot32]

            await Connection.WriteBytesAsync(BitConverter.GetBytes(ulong.MaxValue), SurpriseTradeLockBox, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
                return PokeTradeResult.Aborted;

            if (await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                Log("Trade complete!");
            else
                await ExitTrade(Hub.Config, true, token).ConfigureAwait(false);

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "surprise", SurprisePoke);
            Settings.AddCompletedSurprise();

            return PokeTradeResult.Success;
        }

        private async Task<PokeTradeResult> EndSeedCheckTradeAsync(PokeTradeDetail<PK8> detail, PK8 pk, CancellationToken token)
        {
            await ExitSeedCheckTrade(Hub.Config, token).ConfigureAwait(false);

            detail.TradeFinished(this, pk);

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "seed", pk);

            // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
#pragma warning disable 4014
            Task.Run(() =>
            {
                try
                {
                    ReplyWithSeedCheckResults(detail, pk);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    detail.SendNotification(this, $"Unable to calculate seeds: {ex.Message}\r\n{ex.StackTrace}");
                }
            }, token);
#pragma warning restore 4014

            Settings.AddCompletedSeedCheck();

            return PokeTradeResult.Success;
        }

        private void ReplyWithSeedCheckResults(PokeTradeDetail<PK8> detail, PK8 result)
        {
            detail.SendNotification(this, "Calculating your seed(s)...");

            if (result.IsShiny)
            {
                Log("The Pokémon is already shiny!"); // Do not bother checking for next shiny frame
                detail.SendNotification(this, "This Pokémon is already shiny! Raid seed calculation was not done.");

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "seed", result);

                detail.TradeFinished(this, result);
                return;
            }

            SeedChecker.CalculateAndNotify(result, detail, Hub.Config.SeedCheck, this);
            Log("Seed calculation completed.");
        }

        private void WaitAtBarrierIfApplicable(CancellationToken token)
        {
            if (!ShouldWaitAtBarrier)
                return;
            var opt = Hub.Config.Distribution.SynchronizeBots;
            if (opt == BotSyncOption.NoSync)
                return;

            var timeoutAfter = Hub.Config.Distribution.SynchronizeTimeout;
            if (FailedBarrier == 1) // failed last iteration
                timeoutAfter *= 2; // try to re-sync in the event things are too slow.

            var result = Hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token);

            if (result)
            {
                FailedBarrier = 0;
                return;
            }

            FailedBarrier++;
            Log($"Barrier sync timed out after {timeoutAfter} seconds. Continuing.");
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
                Log($"Joined the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
            }
            else
            {
                Hub.BotSync.Barrier.RemoveParticipant();
                Log($"Left the Barrier. Count: {Hub.BotSync.Barrier.ParticipantCount}");
            }
        }

        private async Task<bool> WaitForPokemonChanged(uint offset, int waitms, int waitInterval, CancellationToken token)
        {
            // check EC and checksum; some pkm may have same EC if shown sequentially
            var oldEC = await Connection.ReadBytesAsync(offset, 8, token).ConfigureAwait(false);
            return await ReadUntilChanged(offset, oldEC, waitms, waitInterval, false, token).ConfigureAwait(false);
        }

        private async Task ExitTrade(PokeTradeHubConfig config, bool unexpected, CancellationToken token)
        {
            if (unexpected)
                Log("Unexpected behavior, recover position");

            int attempts = 0;
            int softBanAttempts = 0;
            while (!await IsOnOverworld(config, token).ConfigureAwait(false))
            {
                var screenID = await GetCurrentScreen(token).ConfigureAwait(false);
                if (screenID == CurrentScreen_Softban)
                {
                    softBanAttempts++;
                    if (softBanAttempts > 10)
                        await ReOpenGame(config, token).ConfigureAwait(false);
                }

                attempts++;
                if (attempts >= 15)
                    break;

                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }
        }

        private async Task ExitSeedCheckTrade(PokeTradeHubConfig config, CancellationToken token)
        {
            // Seed Check Bot doesn't show anything, so it can skip the first B press.
            int attempts = 0;
            while (!await IsOnOverworld(config, token).ConfigureAwait(false))
            {
                attempts++;
                if (attempts >= 15)
                    break;

                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }

            await Task.Delay(3_000, token).ConfigureAwait(false);
        }

        private async Task ResetTradePosition(PokeTradeHubConfig config, CancellationToken token)
        {
            Log("Resetting bot position.");

            // Shouldn't ever be used while not on overworld.
            if (!await IsOnOverworld(config, token).ConfigureAwait(false))
                await ExitTrade(config, true, token).ConfigureAwait(false);

            // Ensure we're searching before we try to reset a search.
            if (!await CheckIfSearchingForLinkTradePartner(token).ConfigureAwait(false))
                return;

            await Click(Y, 2_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
                await Click(A, 1_500, token).ConfigureAwait(false);
            // Extra A press for Japanese.
            if (GameLang == LanguageID.Japanese)
                await Click(A, 1_500, token).ConfigureAwait(false);
            await Click(B, 1_500, token).ConfigureAwait(false);
            await Click(B, 1_500, token).ConfigureAwait(false);
        }

        // Blocks a user from the box during in-game trades.
        protected async Task BlockUser(CancellationToken token)
        {
            Log("Blocking user...");
            await PressAndHold(RSTICK, 0_750, 0_500, token).ConfigureAwait(false);
            await Click(DUP, 0_300, token).ConfigureAwait(false);
            await Click(A, 1_300, token).ConfigureAwait(false);
            await Click(A, 1_300, token).ConfigureAwait(false);
            await Click(DUP, 0_300, token).ConfigureAwait(false);
            await Click(A, 1_100, token).ConfigureAwait(false);
            await Click(A, 1_100, token).ConfigureAwait(false);
        }

        private async Task<bool> LinkTradePartnerFound(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LinkTradeSearchingOffset, 1, token).ConfigureAwait(false);
            return data[0] == 0;
        }

        private async Task<bool> CheckIfSearchingForLinkTradePartner(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LinkTradeSearchingOffset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        private async Task<bool> CheckIfSearchingForSurprisePartner(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(SurpriseTradeSearchOffset, 8, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0) == SurpriseTradeSearch_Searching;
        }

        private async Task<bool> CheckTradePartnerName(TradeMethod tradeMethod, string Name, CancellationToken token)
        {
            var name = await GetTradePartnerName(tradeMethod, token).ConfigureAwait(false);
            return name == Name;
        }

        private async Task<string> GetTradePartnerName(TradeMethod tradeMethod, CancellationToken token)
        {
            var ofs = GetTrainerNameOffset(tradeMethod);
            var data = await Connection.ReadBytesAsync(ofs, 26, token).ConfigureAwait(false);
            return StringConverter.GetString7(data, 0, 26);
        }

        public async Task<ulong> GetTradePartnerNID(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LinkTradePartnerNIDOffset, 8, token).ConfigureAwait(false);
            return BitConverter.ToUInt64(data, 0);
        }
    }
}
