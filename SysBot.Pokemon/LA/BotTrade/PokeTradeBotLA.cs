using System.Linq;
using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsLA;

namespace SysBot.Pokemon
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class PokeTradeBotLA : PokeRoutineExecutor8LA, ICountBot
    {
        private readonly PokeTradeHub<PA8> Hub;
        private readonly TradeSettings TradeSettings;
        private readonly TradeAbuseSettings AbuseSettings;

        public ICountSettings Counts => TradeSettings;

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

        public PokeTradeBotLA(PokeTradeHub<PA8> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
            TradeSettings = hub.Config.Trade;
            AbuseSettings = hub.Config.TradeAbuse;
            DumpSetting = hub.Config.Folder;
        }

        // Cached offsets that stay the same per session.
        private ulong BoxStartOffset;
        private ulong SoftBanOffset;
        private ulong OverworldOffset;
        private ulong TradePartnerNIDOffset;

        // Cached offsets that stay the same per trade.
        private ulong TradePartnerOfferedOffset;

        public override async Task MainLoop(CancellationToken token)
        {
            try
            {
                await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

                Log("Identifying trainer data of the host console.");
                var sav = await IdentifyTrainer(token).ConfigureAwait(false);
                RecentTrainerCache.SetRecentTrainer(sav);
                await InitializeSessionOffsets(token).ConfigureAwait(false);

                Log($"Starting main {nameof(PokeTradeBotLA)} loop.");
                await InnerLoop(sav, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }

            Log($"Ending {nameof(PokeTradeBotLA)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            UpdateBarrier(false);
            await CleanExit(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task InnerLoop(SAV8LA sav, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Config.IterateNextRoutine();
                var task = Config.CurrentRoutineType switch
                {
                    PokeRoutineType.Idle => DoNothing(token),
                    _ => DoTrades(sav, token),
                };
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (SocketException e)
                {
                    if (e.StackTrace != null)
                        Connection.LogError(e.StackTrace);
                    var attempts = Hub.Config.Timings.ReconnectAttempts;
                    var delay = Hub.Config.Timings.ExtraReconnectDelay;
                    var protocol = Config.Connection.Protocol;
                    if (!await TryReconnect(attempts, delay, protocol, token).ConfigureAwait(false))
                        return;
                }
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

        private async Task DoTrades(SAV8LA sav, CancellationToken token)
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

                detail.IsProcessing = true;
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
            if (waitCounter % interval == interval - 1 && Hub.Config.AntiIdle)
                await Click(B, 1_000, token).ConfigureAwait(false);
            else
                await Task.Delay(1_000, token).ConfigureAwait(false);
        }

        protected virtual (PokeTradeDetail<PA8>? detail, uint priority) GetTradeData(PokeRoutineType type)
        {
            if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
                return (detail, priority);
            if (Hub.Queues.TryDequeueLedy(out detail))
                return (detail, PokeTradePriorities.TierFree);
            return (null, PokeTradePriorities.TierFree);
        }

        private async Task PerformTrade(SAV8LA sav, PokeTradeDetail<PA8> detail, PokeRoutineType type, uint priority, CancellationToken token)
        {
            PokeTradeResult result;
            try
            {
                result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);
                if (result == PokeTradeResult.Success)
                    return;
            }
            catch (SocketException socket)
            {
                Log(socket.Message);
                result = PokeTradeResult.ExceptionConnection;
                HandleAbortedTrade(detail, type, priority, result);
                throw; // let this interrupt the trade loop. re-entering the trade loop will recheck the connection.
            }
            catch (Exception e)
            {
                Log(e.Message);
                result = PokeTradeResult.ExceptionInternal;
            }

            HandleAbortedTrade(detail, type, priority, result);
        }

        private void HandleAbortedTrade(PokeTradeDetail<PA8> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
        {
            detail.IsProcessing = false;
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

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8LA sav, PokeTradeDetail<PA8> poke, CancellationToken token)
        {
            // Update Barrier Settings
            UpdateBarrier(poke.IsSynchronized);
            poke.TradeInitialize(this);
            Hub.Config.Stream.EndEnterCode(this);

            if (await CheckIfSoftBanned(SoftBanOffset, token).ConfigureAwait(false))
                await UnSoftBan(token).ConfigureAwait(false);

            var toSend = poke.TradeData;
            if (toSend.Species != 0)
                await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);

            if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            {
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverStart;
            }

            // Speak to the NPC to start a trade.
            Log("Speaking to Simona to start a trade.");
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 0_600, token).ConfigureAwait(false);
            await Click(A, 1_500, token).ConfigureAwait(false);

            Log("Selecting Link Trade.");
            await Click(DRIGHT, 0_500, token).ConfigureAwait(false);
            await Click(A, 1_500, token).ConfigureAwait(false);
            await Click(A, 2_000, token).ConfigureAwait(false);

            // Loading code entry.
            if (poke.Type != PokeTradeType.Random)
                Hub.Config.Stream.StartEnterCode(this);
            await Task.Delay(Hub.Config.Timings.ExtraTimeOpenCodeEntry, token).ConfigureAwait(false);

            var code = poke.Code;
            Log($"Entering Link Trade code: {code:0000 0000}...");
            await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);
            await Click(PLUS, 1_000, token).ConfigureAwait(false);

            poke.TradeSearching(this);

            // Wait for a Trainer...
            var partnerFound = await WaitForTradePartner(token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.RoutineCancel;
            }
            if (!partnerFound)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }

            Hub.Config.Stream.EndEnterCode(this);

            // Some more time to fully enter the trade.
            await Task.Delay(1_000 + Hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false);

            var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
            var trainerNID = await GetTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false);
            RecordUtil<PokeTradeBot>.Record($"Initiating\t{trainerNID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
            Log($"Found Link Trade partner: {tradePartner.TrainerName}-{tradePartner.TID7} (ID: {trainerNID})");

            var partnerCheck = await CheckPartnerReputation(this, poke, trainerNID, tradePartner.TrainerName, AbuseSettings, PreviousUsers, PreviousUsersDistribution, EncounteredUsers, token);
            if (partnerCheck != PokeTradeResult.Success)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return partnerCheck;
            }

            poke.SendNotification(this, $"Found Link Trade partner: {tradePartner.TrainerName}. Waiting for a Pokémon...");

            if (poke.Type == PokeTradeType.Dump)
            {
                var result = await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);
                await ExitTrade(false, token).ConfigureAwait(false);
                return result;
            }

            // Watch their status to indicate they have offered a Pokémon as well.
            var offering = await ReadUntilChanged(TradePartnerOfferedOffset, new byte[] { 0x3 }, 25_000, 1_000, true, true, token).ConfigureAwait(false);
            if (!offering)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            Log("Checking offered Pokémon.");
            // If we got to here, we can read their offered Pokémon.

            // Wait for user input... Needs to be different from the previously offered Pokémon.
            var offered = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (offered == null || offered.Species < 1 || !offered.ChecksumValid)
            {
                Log("Trade ended because trainer offer was rescinded too quickly.");
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerOfferCanceledQuick;
            }

            PokeTradeResult update;
            var trainer = new PartnerDataHolder(0, tradePartner.TrainerName, tradePartner.TID7);
            (toSend, update) = await GetEntityToSend(sav, poke, offered, toSend, trainer, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return update;
            }

            Log("Confirming trade.");
            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult != PokeTradeResult.Success)
            {
                if (tradeResult == PokeTradeResult.TrainerLeft)
                    Log("Trade canceled because trainer left the trade.");
                await ExitTrade(false, token).ConfigureAwait(false);
                return tradeResult;
            }

            if (token.IsCancellationRequested)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.RoutineCancel;
            }

            // Trade was Successful!
            var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
            {
                Log("User did not complete the trade.");
                await ExitTrade(false, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            // As long as we got rid of our inject in b1s1, assume the trade went through.
            Log("User completed the trade.");
            poke.TradeFinished(this, received);

            // Only log if we completed the trade.
            UpdateCountsAndExport(poke, received, toSend);

            await ExitTrade(false, token).ConfigureAwait(false);
            return PokeTradeResult.Success;
        }

        private void UpdateCountsAndExport(PokeTradeDetail<PA8> poke, PA8 received, PA8 toSend)
        {
            var counts = TradeSettings;
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

        private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PA8> detail, CancellationToken token)
        {
            // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
            var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < Hub.Config.Trade.MaxTradeConfirmTime; i++)
            {
                if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                    return PokeTradeResult.TrainerLeft;
                if (await IsUserBeingShifty(detail, token).ConfigureAwait(false))
                    return PokeTradeResult.SuspiciousActivity;
                await Click(A, 1_000, token).ConfigureAwait(false);

                // EC is detectable at the start of the animation.
                var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);
                if (!newEC.SequenceEqual(oldEC))
                {
                    await Task.Delay(30_000, token).ConfigureAwait(false);
                    return PokeTradeResult.Success;
                }
            }
            if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                return PokeTradeResult.TrainerLeft;

            // If we don't detect a B1S1 change, the trade didn't go through in that time.
            return PokeTradeResult.TrainerTooSlow;
        }

        protected virtual async Task<bool> WaitForTradePartner(CancellationToken token)
        {
            Log("Waiting for trainer...");
            int ctr = (Hub.Config.Trade.TradeWaitTime * 1_000) - 2_000;
            await Task.Delay(2_000, token).ConfigureAwait(false);
            while (ctr > 0)
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                var (valid, offset) = await ValidatePointerAll(Offsets.TradePartnerStatusPointer, token).ConfigureAwait(false);
                ctr -= 1_000;
                if (!valid)
                    continue;
                var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 4, token).ConfigureAwait(false);
                if (BitConverter.ToInt32(data, 0) != 2)
                    continue;
                TradePartnerOfferedOffset = offset;
                return true;
            }
            return false;
        }

        private async Task ExitTrade(bool unexpected, CancellationToken token)
        {
            if (unexpected)
                Log("Unexpected behavior, recovering position.");

            int ctr = 120_000;
            while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            {
                if (ctr < 0)
                {
                    await RestartGameLA(token).ConfigureAwait(false);
                    return;
                }

                await Click(B, 1_000, token).ConfigureAwait(false);
                if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                    return;

                var (valid, _) = await ValidatePointerAll(Offsets.TradePartnerStatusPointer, token).ConfigureAwait(false);
                await Click(valid ? A : B, 1_000, token).ConfigureAwait(false);
                if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                    return;

                await Click(B, 1_000, token).ConfigureAwait(false);
                if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                    return;

                ctr -= 3_000;
            }
        }

        // These don't change per session and we access them frequently, so set these each time we start.
        private async Task InitializeSessionOffsets(CancellationToken token)
        {
            Log("Caching session offsets...");
            BoxStartOffset = await SwitchConnection.PointerAll(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
            SoftBanOffset = await SwitchConnection.PointerAll(Offsets.SoftbanPointer, token).ConfigureAwait(false);
            OverworldOffset = await SwitchConnection.PointerAll(Offsets.OverworldPointer, token).ConfigureAwait(false);
            TradePartnerNIDOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerNIDPointer, token).ConfigureAwait(false);
        }

        // todo: future
        protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PA8> detail, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return false;
        }

        private async Task RestartGameLA(CancellationToken token)
        {
            await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
            await InitializeSessionOffsets(token).ConfigureAwait(false);
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PA8> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;

            var pkprev = new PA8();
            var bctr = 0;
            while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
            {
                if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                    break;
                if (bctr++ % 3 == 0)
                    await Click(B, 0_100, token).ConfigureAwait(false);

                // Wait for user input... Needs to be different from the previously offered Pokémon.
                var pk = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
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
                var verbose = $"```{la.Report(true)}```";
                Log($"Shown Pokémon is: {(la.Valid ? "Valid" : "Invalid")}.");

                ctr++;
                var msg = Hub.Config.Trade.DumpTradeLegalityCheck ? verbose: $"File {ctr}";

                // Extra information about trainer data for people requesting with their own trainer data.
                var ot = pk.OT_Name;
                var ot_gender = pk.OT_Gender == 0 ? "Male" : "Female";
                var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
                var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
                msg += $"\n**Trainer Data**\n```OT: {ot}\nOTGender: {ot_gender}\nTID: {tid}\nSID: {sid}```";

                msg += pk.IsShiny ? "\n**This Pokémon is shiny!**" : string.Empty;
                detail.SendNotification(this, pk, msg);
            }

            Log($"Ended Dump loop after processing {ctr} Pokémon.");
            if (ctr == 0)
                return PokeTradeResult.TrainerTooSlow;

            TradeSettings.AddCompletedDumps();
            detail.Notifier.SendNotification(this, detail, $"Dumped {ctr} Pokémon.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank PA8
            return PokeTradeResult.Success;
        }

        private async Task<TradePartnerLA> GetTradePartnerInfo(CancellationToken token)
        {
            var id = await SwitchConnection.PointerPeek(4, Offsets.LinkTradePartnerTIDPointer, token).ConfigureAwait(false);
            var name = await SwitchConnection.PointerPeek(TradePartnerLA.MaxByteLengthStringObject, Offsets.LinkTradePartnerNamePointer, token).ConfigureAwait(false);
            return new TradePartnerLA(id, name);
        }

        protected virtual async Task<(PA8 toSend, PokeTradeResult check)> GetEntityToSend(SAV8LA sav, PokeTradeDetail<PA8> poke, PA8 offered, PA8 toSend, PartnerDataHolder partnerID, CancellationToken token)
        {
            return poke.Type switch
            {
                PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
                PokeTradeType.Clone => await HandleClone(sav, poke, offered, token).ConfigureAwait(false),
                _ => (toSend, PokeTradeResult.Success),
            };
        }

        private async Task<(PA8 toSend, PokeTradeResult check)> HandleClone(SAV8LA sav, PokeTradeDetail<PA8> poke, PA8 offered, CancellationToken token)
        {
            if (Hub.Config.Discord.ReturnPKMs)
                poke.SendNotification(this, offered, "Here's what you showed me!");

            var la = new LegalityAnalysis(offered);
            if (!la.Valid)
            {
                Log($"Clone request (from {poke.Trainer.TrainerName}) has detected an invalid Pokémon: {GameInfo.GetStrings(1).Species[offered.Species]}.");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

                var report = la.Report();
                Log(report);
                poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I am forbidden from cloning this. Exiting trade.");
                poke.SendNotification(this, report);

                return (offered, PokeTradeResult.IllegalTrade);
            }

            var clone = offered.Clone();
            if (Hub.Config.Legality.ResetHOMETracker)
                clone.Tracker = 0;

            poke.SendNotification(this, $"**Cloned your {GameInfo.GetStrings(1).Species[clone.Species]}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
            Log($"Cloned a {(Species)clone.Species}. Waiting for user to change their Pokémon...");

            if (!await CheckCloneChangedOffer(token).ConfigureAwait(false))
            {
                // They get one more chance.
                poke.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");
                if (!await CheckCloneChangedOffer(token).ConfigureAwait(false))
                {
                    Log("Trade partner did not change their Pokémon.");
                    return (offered, PokeTradeResult.TrainerTooSlow);
                }
            }

            // If we got to here, we can read their offered Pokémon.
            var pk2 = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 5_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (pk2 is null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
            {
                Log("Trade partner did not change their Pokémon.");
                return (offered, PokeTradeResult.TrainerTooSlow);
            }

            await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);

            return (clone, PokeTradeResult.Success);
        }

        private async Task<bool> CheckCloneChangedOffer(CancellationToken token)
        {
            // Watch their status to indicate they canceled, then offered a new Pokémon.
            var hovering = await ReadUntilChanged(TradePartnerOfferedOffset, new byte[] { 0x2 }, 25_000, 1_000, true, true, token).ConfigureAwait(false);
            if (!hovering)
            {
                Log("Trade partner did not change their initial offer.");
                await ExitTrade(false, token).ConfigureAwait(false);
                return false;
            }
            var offering = await ReadUntilChanged(TradePartnerOfferedOffset, new byte[] { 0x3 }, 25_000, 1_000, true, true, token).ConfigureAwait(false);
            if (!offering)
            {
                await ExitTrade(false, token).ConfigureAwait(false);
                return false;
            }
            return true;
        }

        private async Task<(PA8 toSend, PokeTradeResult check)> HandleRandomLedy(SAV8LA sav, PokeTradeDetail<PA8> poke, PA8 offered, PA8 toSend, PartnerDataHolder partner, CancellationToken token)
        {
            // Allow the trade partner to do a Ledy swap.
            var config = Hub.Config.Distribution;
            var trade = Hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies);
            if (trade != null)
            {
                if (trade.Type == LedyResponseType.AbuseDetected)
                {
                    var msg = $"Found {partner.TrainerName} has been detected for abusing Ledy trades.";
                    if (AbuseSettings.EchoNintendoOnlineIDLedy)
                        msg += $"\nID: {partner.TrainerOnlineID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.LedyAbuseEchoMention))
                        msg = $"{AbuseSettings.LedyAbuseEchoMention} {msg}";
                    EchoUtil.Echo(msg);

                    return (toSend, PokeTradeResult.SuspiciousActivity);
                }

                toSend = trade.Receive;
                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
            }
            else if (config.LedyQuitIfNoMatch)
            {
                return (toSend, PokeTradeResult.TrainerRequestBad);
            }

            return (toSend, PokeTradeResult.Success);
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
    }
}
