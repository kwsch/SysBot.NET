using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.BasePokeDataOffsetsBS;

namespace SysBot.Pokemon
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class PokeTradeBotBS : PokeRoutineExecutor8BS, ICountBot
    {
        private readonly PokeTradeHub<PB8> Hub;
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

        public PokeTradeBotBS(PokeTradeHub<PB8> hub, PokeBotState cfg) : base(cfg)
        {
            Hub = hub;
            TradeSettings = hub.Config.Trade;
            AbuseSettings = hub.Config.TradeAbuse;
            DumpSetting = hub.Config.Folder;
            lastOffered = new byte[8];
        }

        // Cached offsets that stay the same per session.
        private ulong BoxStartOffset;
        private ulong UnionGamingOffset;
        private ulong UnionTalkingOffset;
        private ulong SoftBanOffset;
        private ulong LinkTradePokemonOffset;

        // Track the last Pokémon we were offered since it persists between trades.
        private byte[] lastOffered;

        public override async Task MainLoop(CancellationToken token)
        {
            try
            {
                await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

                Log("Identifying trainer data of the host console.");
                var sav = await IdentifyTrainer(token).ConfigureAwait(false);
                RecentTrainerCache.SetRecentTrainer(sav);

                await RestartGameIfCantLeaveUnionRoom(token).ConfigureAwait(false);
                await InitializeSessionOffsets(token).ConfigureAwait(false);

                Log($"Starting main {nameof(PokeTradeBotBS)} loop.");
                await InnerLoop(sav, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }

            Log($"Ending {nameof(PokeTradeBotBS)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            UpdateBarrier(false);
            await CleanExit(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task InnerLoop(SAV8BS sav, CancellationToken token)
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

        private async Task DoTrades(SAV8BS sav, CancellationToken token)
        {
            var type = Config.CurrentRoutineType;
            int waitCounter = 0;
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
                if (detail.Type != PokeTradeType.Random || !Hub.Config.Distribution.RemainInUnionRoomBDSP)
                    await RestartGameIfCantLeaveUnionRoom(token).ConfigureAwait(false);
                string tradetype = $" ({detail.Type})";
                Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
                await Task.Delay(500, token).ConfigureAwait(false);
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

        protected virtual (PokeTradeDetail<PB8>? detail, uint priority) GetTradeData(PokeRoutineType type)
        {
            if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
                return (detail, priority);
            if (Hub.Queues.TryDequeueLedy(out detail))
                return (detail, PokeTradePriorities.TierFree);
            return (null, PokeTradePriorities.TierFree);
        }

        private async Task PerformTrade(SAV8BS sav, PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, CancellationToken token)
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

        private void HandleAbortedTrade(PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
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

        private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8BS sav, PokeTradeDetail<PB8> poke, CancellationToken token)
        {
            // Update Barrier Settings
            UpdateBarrier(poke.IsSynchronized);
            poke.TradeInitialize(this);
            Hub.Config.Stream.EndEnterCode(this);

            var distroRemainInRoom = poke.Type == PokeTradeType.Random && Hub.Config.Distribution.RemainInUnionRoomBDSP;

            // If we weren't supposed to remain and started out in the Union Room, ensure we're out of the box.
            if (!distroRemainInRoom && await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                    return PokeTradeResult.RecoverReturnOverworld;
            }

            if (await CheckIfSoftBanned(SoftBanOffset, token).ConfigureAwait(false))
                await UnSoftBan(token).ConfigureAwait(false);

            var toSend = poke.TradeData;
            if (toSend.Species != 0)
                await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);

            // Enter Union Room. Shouldn't do anything if we're already there.
            if (!await EnterUnionRoomWithCode(poke.Type, poke.Code, token).ConfigureAwait(false))
            {
                // We don't know how far we made it in, so restart the game to be safe.
                await RestartGameBDSP(token).ConfigureAwait(false);
                return PokeTradeResult.RecoverEnterUnionRoom;
            }

            await RequestUnionRoomTrade(token).ConfigureAwait(false);
            poke.TradeSearching(this);
            var waitPartner = Hub.Config.Trade.TradeWaitTime;

            // Keep pressing A until we detect someone talking to us.
            while (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false) && waitPartner > 0)
            {
                for (int i = 0; i < 2; ++i)
                    await Click(A, 0_450, token).ConfigureAwait(false);

                if (--waitPartner <= 0)
                    return PokeTradeResult.NoTrainerFound;
            }
            Log("Found a user talking to us!");

            // Keep pressing A until TargetTranerParam (sic) is loaded (when we hit the box).
            while (!await IsPartnerParamLoaded(token).ConfigureAwait(false) && waitPartner > 0)
            {
                for (int i = 0; i < 2; ++i)
                    await Click(A, 0_450, token).ConfigureAwait(false);

                // Can be false if they talked and quit.
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    break;
                if (--waitPartner <= 0)
                    return PokeTradeResult.TrainerTooSlow;
            }
            Log("Entering the box...");

            // Still going through dialog and box opening.
            await Task.Delay(3_000, token).ConfigureAwait(false);

            // Can happen if they quit out of talking to us.
            if (!await IsPartnerParamLoaded(token).ConfigureAwait(false))
                return PokeTradeResult.TrainerTooSlow;

            var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
            var trainerNID = GetFakeNID(tradePartner.TrainerName, tradePartner.TrainerID);
            RecordUtil<PokeTradeBot>.Record($"Initiating\t{trainerNID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
            Log($"Found Link Trade partner: {tradePartner.TrainerName}-{tradePartner.TID7} (ID: {trainerNID}");

            var partnerCheck = await CheckPartnerReputation(this, poke, trainerNID, tradePartner.TrainerName, AbuseSettings, PreviousUsers, PreviousUsersDistribution, EncounteredUsers, token);
            if (partnerCheck != PokeTradeResult.Success)
            {
                // Try to get out of the box.
                if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                    return PokeTradeResult.RecoverReturnOverworld;

                // Leave the Union room if we chose not to stay.
                if (!distroRemainInRoom)
                {
                    Log("Trying to get out of the Union Room.");
                    if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
                        return PokeTradeResult.RecoverReturnOverworld;
                }
                return PokeTradeResult.SuspiciousActivity;
            }

            await Task.Delay(2_000 + Hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false);

            // Confirm Box 1 Slot 1
            if (poke.Type == PokeTradeType.Specific)
            {
                for (int i = 0; i < 5; i++)
                    await Click(A, 0_500, token).ConfigureAwait(false);
            }

            poke.SendNotification(this, $"Found Link Trade partner: {tradePartner.TrainerName}. Waiting for a Pokémon...");

            // Requires at least one trade for this pointer to make sense, so cache it here.
            LinkTradePokemonOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);

            if (poke.Type == PokeTradeType.Dump)
                return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

            // Wait for user input... Needs to be different from the previously offered Pokémon.
            var tradeOffered = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 25_000, 1_000, false, true, token).ConfigureAwait(false);
            if (!tradeOffered)
                return PokeTradeResult.TrainerTooSlow;

            // If we detected a change, they offered something.
            var offered = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (offered.Species == 0 || !offered.ChecksumValid)
                return PokeTradeResult.TrainerTooSlow;
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

            PokeTradeResult update;
            var trainer = new PartnerDataHolder(0, tradePartner.TrainerName, tradePartner.TID7);
            (toSend, update) = await GetEntityToSend(sav, poke, offered, toSend, trainer, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
                return update;

            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult != PokeTradeResult.Success)
                return tradeResult;

            if (token.IsCancellationRequested)
                return PokeTradeResult.RoutineCancel;

            // Trade was Successful!
            var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
            {
                Log("User did not complete the trade.");
                return PokeTradeResult.TrainerTooSlow;
            }

            // As long as we got rid of our inject in b1s1, assume the trade went through.
            Log("User completed the trade.");
            poke.TradeFinished(this, received);

            // Only log if we completed the trade.
            UpdateCountsAndExport(poke, received, toSend);

            // Try to get out of the box.
            if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                return PokeTradeResult.RecoverReturnOverworld;

            // Leave the Union room if we chose not to stay.
            if (!distroRemainInRoom)
            {
                Log("Trying to get out of the Union Room.");
                if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
                    return PokeTradeResult.RecoverReturnOverworld;
            }

            // Sometimes they offered another mon, so store that immediately upon leaving Union Room.
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

            return PokeTradeResult.Success;
        }

        private static ulong GetFakeNID(string trainerName, uint trainerID)
        {
            var nameHash = trainerName.GetHashCode();
            return ((ulong)trainerID << 32) | (uint)nameHash;
        }

        private void UpdateCountsAndExport(PokeTradeDetail<PB8> poke, PB8 received, PB8 toSend)
        {
            var counts = TradeSettings;
            if (poke.Type == PokeTradeType.Random)
                counts.AddCompletedDistribution();
            else
                counts.AddCompletedTrade();

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
            {
                var subfolder = poke.Type.ToString().ToLower();
                DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // received by bot
                if (poke.Type is PokeTradeType.Specific)
                    DumpPokemon(DumpSetting.DumpFolder, "traded", toSend); // sent to partner
            }
        }

        private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PB8> detail, CancellationToken token)
        {
            // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
            var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

            await Click(A, 3_000, token).ConfigureAwait(false);
            for (int i = 0; i < Hub.Config.Trade.MaxTradeConfirmTime; i++)
            {
                if (await IsUserBeingShifty(detail, token).ConfigureAwait(false))
                    return PokeTradeResult.SuspiciousActivity;
                // We're no longer talking, so they probably quit on us.
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    return PokeTradeResult.TrainerTooSlow;
                await Click(A, 1_000, token).ConfigureAwait(false);

                // EC is detectable at the start of the animation.
                var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);
                if (!newEC.SequenceEqual(oldEC))
                {
                    await Task.Delay(25_000, token).ConfigureAwait(false);
                    return PokeTradeResult.Success;
                }
            }

            // If we don't detect a B1S1 change, the trade didn't go through in that time.
            return PokeTradeResult.TrainerTooSlow;
        }

        private async Task<bool> EnterUnionRoomWithCode(PokeTradeType tradeType, int tradeCode, CancellationToken token)
        {
            // Already in Union Room.
            if (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
                return true;

            // Open y-comm and select global room
            await Click(Y, 1_000 + Hub.Config.Timings.ExtraTimeOpenYMenu, token).ConfigureAwait(false);
            await Click(DRIGHT, 0_400, token).ConfigureAwait(false);

            // French has one less menu
            if (GameLang is not LanguageID.French)
            {
                await Click(A, 0_050, token).ConfigureAwait(false);
                await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
            }

            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);

            // Japanese has one extra menu
            if (GameLang is LanguageID.Japanese)
            {
                await Click(A, 0_050, token).ConfigureAwait(false);
                await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
            }

            await Click(A, 1_000, token).ConfigureAwait(false); // Would you like to enter? Screen

            Log("Selecting Link Code room.");
            // Link code selection index
            await Click(DDOWN, 0_200, token).ConfigureAwait(false);
            await Click(DDOWN, 0_200, token).ConfigureAwait(false);

            Log("Connecting to internet.");
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 2_000, 0, token).ConfigureAwait(false);

            // Extra menus.
            if (GameLang is LanguageID.German or LanguageID.Italian or LanguageID.Korean)
            {
                await Click(A, 0_050, token).ConfigureAwait(false);
                await PressAndHold(A, 0_750, 0, token).ConfigureAwait(false);
            }

            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);

            // Would you like to save your adventure so far?
            await Click(A, 0_500, token).ConfigureAwait(false);
            await Click(A, 0_500, token).ConfigureAwait(false);

            Log("Saving the game.");
            // Agree and save the game.
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 6_500, 0, token).ConfigureAwait(false);

            if (tradeType != PokeTradeType.Random)
                Hub.Config.Stream.StartEnterCode(this);
            Log($"Entering Link Trade code: {tradeCode:0000 0000}...");
            await EnterLinkCode(tradeCode, Hub.Config, token).ConfigureAwait(false);

            // Wait for Barrier to trigger all bots simultaneously.
            WaitAtBarrierIfApplicable(token);
            await Click(PLUS, 0_600, token).ConfigureAwait(false);
            Hub.Config.Stream.EndEnterCode(this);
            Log("Entering the Union Room.");

            // Wait until we're past the communication message.
            int tries = 100;
            while (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                await Click(A, 0_300, token).ConfigureAwait(false);

                if (--tries < 1)
                    return false;
            }

            await Task.Delay(1_300 + Hub.Config.Timings.ExtraTimeJoinUnionRoom, token).ConfigureAwait(false);

            return true; // We've made it into the room and are ready to request.
        }

        private async Task RequestUnionRoomTrade(CancellationToken token)
        {
            // Y-button trades always put us in a place where we can open the call menu without having to move.
            Log("Attempting to open the Y menu.");
            await Click(Y, 1_000, token).ConfigureAwait(false);
            await Click(A, 0_400, token).ConfigureAwait(false);
            await Click(DDOWN, 0_400, token).ConfigureAwait(false);
            await Click(DDOWN, 0_400, token).ConfigureAwait(false);
            await Click(A, 0_100, token).ConfigureAwait(false);
        }

        // These don't change per session and we access them frequently, so set these each time we start.
        private async Task InitializeSessionOffsets(CancellationToken token)
        {
            Log("Caching session offsets...");
            BoxStartOffset = await SwitchConnection.PointerAll(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
            UnionGamingOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkIsGamingPointer, token).ConfigureAwait(false);
            UnionTalkingOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkIsTalkingPointer, token).ConfigureAwait(false);
            SoftBanOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkPenaltyPointer, token).ConfigureAwait(false);
        }

        // todo: future
        protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PB8> detail, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return false;
        }

        private async Task RestartGameIfCantLeaveUnionRoom(CancellationToken token)
        {
            if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
                await RestartGameBDSP(token).ConfigureAwait(false);
        }

        private async Task RestartGameBDSP(CancellationToken token)
        {
            await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
            await InitializeSessionOffsets(token).ConfigureAwait(false);
        }

        private async Task<bool> EnsureOutsideOfUnionRoom(CancellationToken token)
        {
            if (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
                return true;

            if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                return false;
            if (!await ExitUnionRoomToOverworld(token).ConfigureAwait(false))
                return false;
            return true;
        }

        private async Task<bool> ExitBoxToUnionRoom(CancellationToken token)
        {
            if (await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
            {
                Log("Exiting box...");
                int tries = 30;
                while (await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                {
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                        break;
                    await Click(DUP, 0_200, token).ConfigureAwait(false);
                    await Click(A, 0_500, token).ConfigureAwait(false);
                    // Keeps regular quitting a little faster, only need this for trade evolutions + moves.
                    if (tries < 10)
                        await Click(B, 0_500, token).ConfigureAwait(false);
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    tries--;
                    if (tries < 0)
                        return false;
                }
            }
            await Task.Delay(2_000, token).ConfigureAwait(false);
            return true;
        }

        private async Task<bool> ExitUnionRoomToOverworld(CancellationToken token)
        {
            if (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                Log("Exiting Union Room...");
                for (int i = 0; i < 3; ++i)
                    await Click(B, 0_200, token).ConfigureAwait(false);

                await Click(Y, 1_000, token).ConfigureAwait(false);
                await Click(DDOWN, 0_200, token).ConfigureAwait(false);
                for (int i = 0; i < 3; ++i)
                    await Click(A, 0_400, token).ConfigureAwait(false);

                int tries = 10;
                while (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
                {
                    await Task.Delay(0_400, token).ConfigureAwait(false);
                    tries--;
                    if (tries < 0)
                        return false;
                }
                await Task.Delay(3_000 + Hub.Config.Timings.ExtraTimeLeaveUnionRoom, token).ConfigureAwait(false);
            }
            return true;
        }

        private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PB8> detail, CancellationToken token)
        {
            int ctr = 0;
            var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
            var start = DateTime.Now;

            var bctr = 0;
            while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
            {
                // We're no longer talking, so they probably quit on us.
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    break;
                if (bctr++ % 3 == 0)
                    await Click(B, 0_100, token).ConfigureAwait(false);

                // Wait for user input... Needs to be different from the previously offered Pokémon.
                var tradeOffered = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 3_000, 1_000, false, true, token).ConfigureAwait(false);
                if (!tradeOffered)
                    continue;

                // If we detected a change, they offered something.
                var pk = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
                var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);
                if (pk.Species < 1 || !pk.ChecksumValid || lastOffered == newEC)
                    continue;
                lastOffered = newEC;

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
                var msg = Hub.Config.Trade.DumpTradeLegalityCheck ? verbose : $"File {ctr}";

                // Extra information about trainer data for people requesting with their own trainer data.
                var ot = pk.OT_Name;
                var ot_gender = pk.OT_Gender == 0 ? "Male" : "Female";
                var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
                var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
                msg += $"\n**Trainer Data**\n```OT: {ot}\nOTGender: {ot_gender}\nTID: {tid}\nSID: {sid}```";

                // Extra information for shiny eggs, because of people dumping to skip hatching.
                var eggstring = pk.IsEgg ? "Egg " : string.Empty;
                msg += pk.IsShiny ? $"\n**This Pokémon {eggstring}is shiny!**" : string.Empty;
                detail.SendNotification(this, pk, msg);
            }

            Log($"Ended Dump loop after processing {ctr} Pokémon.");
            if (ctr == 0)
                return PokeTradeResult.TrainerTooSlow;

            TradeSettings.AddCompletedDumps();
            detail.Notifier.SendNotification(this, detail, $"Dumped {ctr} Pokémon.");
            detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
            return PokeTradeResult.Success;
        }

        private async Task<TradePartnerBS> GetTradePartnerInfo(CancellationToken token)
        {
            var id = await SwitchConnection.PointerPeek(4, Offsets.LinkTradePartnerIDPointer, token).ConfigureAwait(false);
            var name = await SwitchConnection.PointerPeek(TradePartnerBS.MaxByteLengthStringObject, Offsets.LinkTradePartnerNamePointer, token).ConfigureAwait(false);
            return new TradePartnerBS(id, name);
        }

        protected virtual async Task<(PB8 toSend, PokeTradeResult check)> GetEntityToSend(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PB8 toSend, PartnerDataHolder partnerID, CancellationToken token)
        {
            return poke.Type switch
            {
                PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
                _ => (toSend, PokeTradeResult.Success),
            };
        }

        private async Task<(PB8 toSend, PokeTradeResult check)> HandleRandomLedy(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PB8 toSend, PartnerDataHolder partner, CancellationToken token)
        {
            // Allow the trade partner to do a Ledy swap.
            var config = Hub.Config.Distribution;
            var trade = Hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies);
            if (trade != null)
            {
                if (trade.Type == LedyResponseType.AbuseDetected)
                {
                    var msg = $"Found {partner.TrainerName} has been detected for abusing Ledy trades.";
                    EchoUtil.Echo(msg);

                    return (toSend, PokeTradeResult.SuspiciousActivity);
                }

                toSend = trade.Receive;
                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
                await Task.Delay(2_500, token).ConfigureAwait(false);
            }
            else if (config.LedyQuitIfNoMatch)
            {
                return (toSend, PokeTradeResult.TrainerRequestBad);
            }

            for (int i = 0; i < 5; i++)
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
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
