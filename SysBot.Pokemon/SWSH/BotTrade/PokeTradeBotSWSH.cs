using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsSWSH;

namespace SysBot.Pokemon;

public class PokeTradeBotSWSH(PokeTradeHub<PK8> hub, PokeBotState Config) : PokeRoutineExecutor8SWSH(Config), ICountBot
{
    public static ISeedSearchHandler<PK8> SeedChecker { get; set; } = new NoSeedSearchHandler<PK8>();

    private readonly TradeSettings TradeSettings = hub.Config.Trade;
    private readonly PokeTradeHub<PK8> Hub;
    private readonly TradeAbuseSettings AbuseSettings = hub.Config.TradeAbuse;

    public ICountSettings Counts => TradeSettings;

    /// <summary>
    /// Folder to dump received trade data to.
    /// </summary>
    /// <remarks>If null, will skip dumping.</remarks>
    private readonly IDumper DumpSetting = hub.Config.Folder;

    /// <summary>
    /// Synchronized start for multiple bots.
    /// </summary>
    public bool ShouldWaitAtBarrier { get; private set; }

    /// <summary>
    /// Tracks failed synchronized starts to attempt to re-sync.
    /// </summary>
    public int FailedBarrier { get; private set; }

    // Cached offsets that stay the same per session.
    private ulong OverworldOffset;

    public override async Task MainLoop(CancellationToken token)
    {
        try
        {
            await InitializeHardware(hub.Config.Trade, token).ConfigureAwait(false);

            Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            RecentTrainerCache.SetRecentTrainer(sav);
            await InitializeSessionOffsets(token).ConfigureAwait(false);

            Log($"Starting main {nameof(PokeTradeBotSWSH)} loop.");
            await InnerLoop(sav, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log(e.Message);
        }

        Log($"Ending {nameof(PokeTradeBotSWSH)} loop.");
        await HardStop().ConfigureAwait(false);
    }

    public override Task HardStop()
    {
        UpdateBarrier(false);
        return CleanExit(CancellationToken.None);
    }
    public override async Task RebootAndStop(CancellationToken t)
    {
        await ReOpenGame(new PokeTradeHubConfig(), t).ConfigureAwait(false);
        await HardStop().ConfigureAwait(false);

        await Task.Delay(2_000, t).ConfigureAwait(false);
        if (!t.IsCancellationRequested)
        {
            Log("Restarting the main loop.");
            await MainLoop(t).ConfigureAwait(false);
        }
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
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                if (e.StackTrace != null)
                    Connection.LogError(e.StackTrace);
                var attempts = hub.Config.Timings.ReconnectAttempts;
                var delay = hub.Config.Timings.ExtraReconnectDelay;
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
            if (waitCounter % 10 == 0 && hub.Config.AntiIdle)
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

            detail.IsProcessing = true;
            string tradetype = $" ({detail.Type})";
            Log($"Starting next {type}{tradetype} Bot Trade. Getting data...");
            hub.Config.Stream.StartTrade(this, detail, hub);
            hub.Queues.StartTrade(this, detail);

            await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);
        }
    }

    private Task WaitForQueueStep(int waitCounter, CancellationToken token)
    {
        if (waitCounter == 0)
        {
            // Updates the assets.
            hub.Config.Stream.IdleAssets(this);
            Log("Nothing to check, waiting for new users...");
        }

        const int interval = 10;
        if (waitCounter % interval == interval - 1 && hub.Config.AntiIdle)
            return Click(B, 1_000, token);
        return Task.Delay(1_000, token);
    }

    protected virtual (PokeTradeDetail<PK8>? detail, uint priority) GetTradeData(PokeRoutineType type)
    {
        if (hub.Queues.TryDequeue(type, out var detail, out var priority))
            return (detail, priority);
        if (hub.Queues.TryDequeueLedy(out detail))
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
    private void HandleAbortedTrade(PokeTradeDetail<PK8> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
    {
        detail.IsProcessing = false;
        if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
        {
            detail.IsRetry = true;
            hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
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
            var pkm = hub.Ledy.Pool.GetRandomSurprise();
            await EnsureConnectedToYComm(OverworldOffset, hub.Config, token).ConfigureAwait(false);
            var _ = await PerformSurpriseTrade(sav, pkm, token).ConfigureAwait(false);
        }
    }

    private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8SWSH sav, PokeTradeDetail<PK8> poke, CancellationToken token)
    {
        // Update Barrier Settings
        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);
        await EnsureConnectedToYComm(OverworldOffset, hub.Config, token).ConfigureAwait(false);
        hub.Config.Stream.EndEnterCode(this);

        if (await CheckIfSoftBanned(token).ConfigureAwait(false))
            await UnSoftBan(token).ConfigureAwait(false);

        var toSend = poke.TradeData;
        if (toSend.Species != 0)
            await SetBoxPokemon(toSend, 0, 0, token, sav).ConfigureAwait(false);

        if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            await ExitTrade(true, token).ConfigureAwait(false);
            return PokeTradeResult.RecoverStart;
        }

        while (await CheckIfSearchingForLinkTradePartner(token).ConfigureAwait(false))
        {
            Log("Still searching, resetting bot position.");
            await ResetTradePosition(token).ConfigureAwait(false);
        }

        Log("Opening Y-Comm menu.");
        await Click(Y, 2_000, token).ConfigureAwait(false);

        Log("Selecting Link Trade.");
        await Click(A, 1_500, token).ConfigureAwait(false);

        Log("Selecting Link Trade code.");
        await Click(DDOWN, 500, token).ConfigureAwait(false);

        for (int i = 0; i < 2; i++)
            await Click(A, 1_500, token).ConfigureAwait(false);

        // All other languages require an extra A press at this menu.
        if (GameLang != LanguageID.English && GameLang != LanguageID.Spanish)
            await Click(A, 1_500, token).ConfigureAwait(false);

        // Loading Screen
        if (poke.Type != PokeTradeType.Random)
            hub.Config.Stream.StartEnterCode(this);
        await Task.Delay(hub.Config.Timings.ExtraTimeOpenCodeEntry, token).ConfigureAwait(false);

        var code = poke.Code;
        Log($"Entering Link Trade code: {code:0000 0000}...");
        await EnterLinkCode(code, hub.Config, token).ConfigureAwait(false);

        // Wait for Barrier to trigger all bots simultaneously.
        WaitAtBarrierIfApplicable(token);
        await Click(PLUS, 1_000, token).ConfigureAwait(false);

        hub.Config.Stream.EndEnterCode(this);

        // Confirming and return to overworld.
        var delay_count = 0;
        while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            if (delay_count++ >= 5)
            {
                // Too many attempts, recover out of the trade.
                await ExitTrade(true, token).ConfigureAwait(false);
                return PokeTradeResult.RecoverPostLinkCode;
            }

            for (int i = 0; i < 5; i++)
                await Click(A, 0_800, token).ConfigureAwait(false);
        }

        poke.TradeSearching(this);
        await Task.Delay(0_500, token).ConfigureAwait(false);

        // Wait for a Trainer...
        var partnerFound = await WaitForTradePartnerOffer(token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
        {
            return PokeTradeResult.RoutineCancel;
        }
        if (!partnerFound)
        {
            await ResetTradePosition(token).ConfigureAwait(false);
            return PokeTradeResult.NoTrainerFound;
        }

        // Select Pokémon
        // pkm already injected to b1s1
        await Task.Delay(5_500 + hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false); // necessary delay to get to the box properly

        var trainerName = await GetTradePartnerName(TradeMethod.LinkTrade, token).ConfigureAwait(false);
        var trainerTID = await GetTradePartnerTID7(TradeMethod.LinkTrade, token).ConfigureAwait(false);
        var trainerNID = await GetTradePartnerNID(token).ConfigureAwait(false);
        RecordUtil<PokeTradeBotSWSH>.Record($"Initiating\t{trainerNID:X16}\t{trainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
        Log($"Found Link Trade partner: {trainerName}-{trainerTID} (ID: {trainerNID})");

        var tradeCodeStorage = new TradeCodeStorage();
        var existingTradeDetails = tradeCodeStorage.GetTradeDetails(poke.Trainer.ID);

        bool shouldUpdateOT = existingTradeDetails?.OT != trainerName;
        bool shouldUpdateTID = existingTradeDetails?.TID != int.Parse(trainerTID);

        if (shouldUpdateOT || shouldUpdateTID)
        {
            tradeCodeStorage.UpdateTradeDetails(poke.Trainer.ID, shouldUpdateOT ? trainerName : existingTradeDetails.OT, shouldUpdateTID ? int.Parse(trainerTID) : existingTradeDetails.TID);
        }

        if (!await IsInBox(token).ConfigureAwait(false))
        {
            await ExitTrade(true, token).ConfigureAwait(false);
            return PokeTradeResult.RecoverOpenBox;
        }

        if (hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT)
        {
            await SetPkmWithSwappedIDDetails(toSend, trainerName, sav, token);
        }

        // Confirm Box 1 Slot 1
        if (poke.Type == PokeTradeType.Specific)
        {
            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);
        }

        poke.SendNotification(this, $"Found Link Trade partner: {trainerName}. TID: {trainerTID} Waiting for a Pokémon...");

        if (poke.Type == PokeTradeType.Dump)
            return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

        // Wait for User Input...
        var offered = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 25_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
        var oldEC = await Connection.ReadBytesAsync(LinkTradePartnerPokemonOffset, 4, token).ConfigureAwait(false);
        if (offered is null)
        {
            await ExitSeedCheckTrade(token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        if (poke.Type == PokeTradeType.Seed)
        {
            // Immediately exit, we aren't trading anything.
            return await EndSeedCheckTradeAsync(poke, offered, token).ConfigureAwait(false);
        }

        var trainer = new PartnerDataHolder(trainerNID, trainerName, trainerTID);
        (toSend, PokeTradeResult update) = await GetEntityToSend(sav, poke, offered, oldEC, toSend, trainer, token).ConfigureAwait(false);
        if (update != PokeTradeResult.Success)
        {
            await ExitTrade(false, token).ConfigureAwait(false);
            return update;
        }

        var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
        if (tradeResult != PokeTradeResult.Success)
        {
            await ExitTrade(false, token).ConfigureAwait(false);
            return tradeResult;
        }

        if (token.IsCancellationRequested)
        {
            await ExitTrade(false, token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }

        // Trade was Successful!
        var received = await ReadBoxPokemon(0, 0, token).ConfigureAwait(false);
        // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
        if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
        {
            Log("User did not complete the trade.");
            RecordUtil<PokeTradeBotSWSH>.Record($"Cancelled\t{trainerNID:X16}\t{trainerName}\t{poke.Trainer.TrainerName}\\t{poke.ID}\t{toSend.EncryptionConstant:X8}\t{offered.EncryptionConstant:X8}");
            await ExitTrade(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        // As long as we got rid of our inject in b1s1, assume the trade went through.
        Log("User completed the trade.");
        poke.TradeFinished(this, received);

        RecordUtil<PokeTradeBotSWSH>.Record($"Finished\t{trainerNID:X16}\t{toSend.EncryptionConstant:X8}\t{received.EncryptionConstant:X8}");

        // Only log if we completed the trade.
        UpdateCountsAndExport(poke, received, toSend);

        // Log for Trade Abuse tracking.
        LogSuccessfulTrades(poke, trainerNID, trainerName);

        await ExitTrade(false, token).ConfigureAwait(false);
        return PokeTradeResult.Success;
    }

    protected virtual Task<bool> WaitForTradePartnerOffer(CancellationToken token)
    {
        Log("Waiting for trainer...");
        return WaitForPokemonChanged(LinkTradePartnerPokemonOffset, hub.Config.Trade.TradeConfiguration.TradeWaitTime * 1_000, 0_200, token);
    }

    private void UpdateCountsAndExport(PokeTradeDetail<PK8> poke, PK8 received, PK8 toSend)
    {
        var counts = TradeSettings;
        if (poke.Type == PokeTradeType.Random)
            counts.CountStatsSettings.AddCompletedDistribution();
        else if (poke.Type == PokeTradeType.FixOT)
            counts.CountStatsSettings.AddCompletedFixOTs();
        else if (poke.Type == PokeTradeType.Clone)
            counts.CountStatsSettings.AddCompletedClones();
        else
            counts.CountStatsSettings.AddCompletedTrade();

        if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
        {
            var subfolder = poke.Type.ToString().ToLower();
            var service = poke.Notifier.GetType().ToString().ToLower();
            var tradedFolder = service.Contains("twitch") ? Path.Combine("traded", "twitch") : service.Contains("discord") ? Path.Combine("traded", "discord") : "traded";
            DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // received by bot
            if (poke.Type is PokeTradeType.Specific or PokeTradeType.Clone or PokeTradeType.FixOT)
                DumpPokemon(DumpSetting.DumpFolder, tradedFolder, toSend); // sent to partner
        }
    }

    private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PK8> detail, CancellationToken token)
    {
        // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
        var oldEC = await Connection.ReadBytesAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

        await Click(A, 3_000, token).ConfigureAwait(false);
        for (int i = 0; i < hub.Config.Trade.TradeConfiguration.MaxTradeConfirmTime; i++)
        {
            // If we are in a Trade Evolution/Pokédex Entry and the Trade Partner quits, we land on the Overworld
            if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                return PokeTradeResult.TrainerLeft;
            if (await IsUserBeingShifty(detail, token).ConfigureAwait(false))
                return PokeTradeResult.SuspiciousActivity;
            await Click(A, 1_000, token).ConfigureAwait(false);

            // EC is detectable at the start of the animation.
            var newEC = await Connection.ReadBytesAsync(BoxStartOffset, 8, token).ConfigureAwait(false);
            if (!newEC.SequenceEqual(oldEC))
            {
                await Task.Delay(25_000, token).ConfigureAwait(false);
                return PokeTradeResult.Success;
            }
        }

        if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            return PokeTradeResult.TrainerLeft;

        return PokeTradeResult.Success;
    }

    protected virtual async Task<(PK8 toSend, PokeTradeResult check)> GetEntityToSend(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, byte[] oldEC, PK8 toSend, PartnerDataHolder partnerID, CancellationToken token)
    {
        return poke.Type switch
        {
            PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
            PokeTradeType.Clone => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
            PokeTradeType.FixOT => await HandleFixOT(sav, poke, offered, partnerID, token).ConfigureAwait(false),
            _ => (toSend, PokeTradeResult.Success),
        };
    }

    private async Task<(PK8 toSend, PokeTradeResult check)> HandleClone(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, byte[] oldEC, CancellationToken token)
    {
        if (hub.Config.Discord.ReturnPKMs)
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
        if (hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        poke.SendNotification(this, $"**Cloned your {GameInfo.GetStrings(1).Species[clone.Species]}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
        Log($"Cloned a {(Species)clone.Species}. Waiting for user to change their Pokémon...");

        // Separate this out from WaitForPokemonChanged since we compare to old EC from original read.
        var partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, oldEC, 15_000, 0_200, false, token).ConfigureAwait(false);

        if (!partnerFound)
        {
            poke.SendNotification(this, "**HEY CHANGE IT NOW OR I AM LEAVING!!!**");
            // They get one more chance.
            partnerFound = await ReadUntilChanged(LinkTradePartnerPokemonOffset, oldEC, 15_000, 0_200, false, token).ConfigureAwait(false);
        }

        var pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
        if (!partnerFound || pk2 == null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
        {
            Log("Trade partner did not change their Pokémon.");
            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemon(clone, 0, 0, token, sav).ConfigureAwait(false);

        for (int i = 0; i < 5; i++)
            await Click(A, 0_500, token).ConfigureAwait(false);

        return (clone, PokeTradeResult.Success);
    }

    private async Task<(PK8 toSend, PokeTradeResult check)> HandleRandomLedy(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, PK8 toSend, PartnerDataHolder partner, CancellationToken token)
    {
        // Allow the trade partner to do a Ledy swap.
        var config = hub.Config.Distribution;
        var trade = hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies);
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
            await Click(A, 0_800, token).ConfigureAwait(false);
            await SetBoxPokemon(toSend, 0, 0, token, sav).ConfigureAwait(false);
            await Task.Delay(2_500, token).ConfigureAwait(false);
        }
        else if (config.LedyQuitIfNoMatch)
        {
            return (toSend, PokeTradeResult.TrainerRequestBad);
        }

        for (int i = 0; i < 5; i++)
        {
            if (await IsUserBeingShifty(poke, token).ConfigureAwait(false))
                return (toSend, PokeTradeResult.SuspiciousActivity);
            await Click(A, 0_500, token).ConfigureAwait(false);
        }

        return (toSend, PokeTradeResult.Success);
    }

    // For pointer offsets that don't change per session are accessed frequently, so set these each time we start.
    private async Task InitializeSessionOffsets(CancellationToken token)
    {
        Log("Caching session offsets...");
        OverworldOffset = await SwitchConnection.PointerAll(Offsets.OverworldPointer, token).ConfigureAwait(false);
    }

    protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PK8> detail, CancellationToken token)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return false;
    }

    private async Task RestartGameSWSH(CancellationToken token)
    {
        await ReOpenGame(hub.Config, token).ConfigureAwait(false);
        await InitializeSessionOffsets(token).ConfigureAwait(false);
    }

    private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PK8> detail, CancellationToken token)
    {
        int ctr = 0;
        var time = TimeSpan.FromSeconds(hub.Config.Trade.TradeConfiguration.MaxDumpTradeTime);
        var start = DateTime.Now;
        var pkprev = new PK8();
        var bctr = 0;
        while (ctr < hub.Config.Trade.TradeConfiguration.MaxDumpsPerTrade && DateTime.Now - start < time)
        {
            if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                break;
            if (bctr++ % 3 == 0)
                await Click(B, 0_100, token).ConfigureAwait(false);

            var pk = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 3_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
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
            var msg = hub.Config.Trade.TradeConfiguration.DumpTradeLegalityCheck ? verbose : $"File {ctr}";

            // Extra information about trainer data for people requesting with their own trainer data.
            var ot = pk.OriginalTrainerName;
            var ot_gender = pk.OriginalTrainerGender == 0 ? "Male" : "Female";
            var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
            var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
            msg += $"\n**Trainer Data**\n```OT: {ot}\nOTGender: {ot_gender}\nTID: {tid}\nSID: {sid}```";

            // Extra information for shiny eggs, because of people dumping to skip hatching.
            var eggstring = pk.IsEgg ? "Egg " : string.Empty;
            msg += pk.IsShiny ? $"\n**This Pokémon {eggstring}is shiny!**" : string.Empty;
            detail.SendNotification(this, pk, msg);
        }

        Log($"Ended Dump loop after processing {ctr} Pokémon.");
        await ExitSeedCheckTrade(token).ConfigureAwait(false);
        if (ctr == 0)
            return PokeTradeResult.TrainerTooSlow;

        TradeSettings.CountStatsSettings.AddCompletedDumps();
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
            await UnSoftBan(token).ConfigureAwait(false);

        Log("Starting next Surprise Trade. Getting data...");
        await SetBoxPokemon(pkm, 0, 0, token, sav).ConfigureAwait(false);

        if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            await ExitTrade(true, token).ConfigureAwait(false);
            return PokeTradeResult.RecoverStart;
        }

        if (await CheckIfSearchingForSurprisePartner(token).ConfigureAwait(false))
        {
            Log("Still searching, resetting bot position.");
            await ResetTradePosition(token).ConfigureAwait(false);
        }

        Log("Opening Y-Comm menu.");
        await Click(Y, 1_500, token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
            return PokeTradeResult.RoutineCancel;

        Log("Selecting Surprise Trade.");
        await Click(DDOWN, 0_500, token).ConfigureAwait(false);
        await Click(A, 2_000, token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
            return PokeTradeResult.RoutineCancel;

        await Task.Delay(0_750, token).ConfigureAwait(false);

        if (!await IsInBox(token).ConfigureAwait(false))
        {
            await ExitTrade(true, token).ConfigureAwait(false);
            return PokeTradeResult.RecoverPostLinkCode;
        }

        Log($"Selecting Pokémon: {pkm.FileName}");
        // Box 1 Slot 1; no movement required.
        await Click(A, 0_700, token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
            return PokeTradeResult.RoutineCancel;

        Log("Confirming...");
        while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            await Click(A, 0_800, token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
            return PokeTradeResult.RoutineCancel;

        // Let Surprise Trade be sent out before checking if we're back to the Overworld.
        await Task.Delay(3_000, token).ConfigureAwait(false);

        if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            await ExitTrade(true, token).ConfigureAwait(false);
            return PokeTradeResult.RecoverReturnOverworld;
        }

        // Wait 30 Seconds for Trainer...
        Log("Waiting for Surprise Trade partner...");

        // Wait for an offer...
        var oldEC = await Connection.ReadBytesAsync(SurpriseTradeSearchOffset, 4, token).ConfigureAwait(false);
        var partnerFound = await ReadUntilChanged(SurpriseTradeSearchOffset, oldEC, hub.Config.Trade.TradeConfiguration.TradeWaitTime * 1_000, 0_200, false, token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
            return PokeTradeResult.RoutineCancel;

        if (!partnerFound)
        {
            await ResetTradePosition(token).ConfigureAwait(false);
            return PokeTradeResult.NoTrainerFound;
        }

        // Let the game flush the results and de-register from the online surprise trade queue.
        await Task.Delay(7_000, token).ConfigureAwait(false);

        var TrainerName = await GetTradePartnerName(TradeMethod.SurpriseTrade, token).ConfigureAwait(false);
        var TrainerTID = await GetTradePartnerTID7(TradeMethod.SurpriseTrade, token).ConfigureAwait(false);
        var SurprisePoke = await ReadSurpriseTradePokemon(token).ConfigureAwait(false);

        Log($"Found Surprise Trade partner: {TrainerName}-{TrainerTID}, Pokémon: {(Species)SurprisePoke.Species}");

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
            return PokeTradeResult.RoutineCancel;

        if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            Log("Trade complete!");
        else
            await ExitTrade(true, token).ConfigureAwait(false);

        if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
            DumpPokemon(DumpSetting.DumpFolder, "surprise", SurprisePoke);
        TradeSettings.CountStatsSettings.AddCompletedSurprise();

        return PokeTradeResult.Success;
    }

    private async Task<PokeTradeResult> EndSeedCheckTradeAsync(PokeTradeDetail<PK8> detail, PK8 pk, CancellationToken token)
    {
        await ExitSeedCheckTrade(token).ConfigureAwait(false);

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
            catch (Exception ex)
            {
                detail.SendNotification(this, $"Unable to calculate seeds: {ex.Message}\r\n{ex.StackTrace}");
            }
        }, token);
#pragma warning restore 4014

        TradeSettings.CountStatsSettings.AddCompletedSeedCheck();

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

        SeedChecker.CalculateAndNotify(result, detail, hub.Config.SeedCheckSWSH, this);
        Log("Seed calculation completed.");
    }

    private void WaitAtBarrierIfApplicable(CancellationToken token)
    {
        if (!ShouldWaitAtBarrier)
            return;
        var opt = hub.Config.Distribution.SynchronizeBots;
        if (opt == BotSyncOption.NoSync)
            return;

        var timeoutAfter = hub.Config.Distribution.SynchronizeTimeout;
        if (FailedBarrier == 1) // failed last iteration
            timeoutAfter *= 2; // try to re-sync in the event things are too slow.

        var result = hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token);

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
            hub.BotSync.Barrier.AddParticipant();
            Log($"Joined the Barrier. Count: {hub.BotSync.Barrier.ParticipantCount}");
        }
        else
        {
            hub.BotSync.Barrier.RemoveParticipant();
            Log($"Left the Barrier. Count: {hub.BotSync.Barrier.ParticipantCount}");
        }
    }

    private async Task<bool> WaitForPokemonChanged(uint offset, int waitms, int waitInterval, CancellationToken token)
    {
        // check EC and checksum; some pkm may have same EC if shown sequentially
        var oldEC = await Connection.ReadBytesAsync(offset, 8, token).ConfigureAwait(false);
        return await ReadUntilChanged(offset, oldEC, waitms, waitInterval, false, token).ConfigureAwait(false);
    }

    private async Task ExitTrade(bool unexpected, CancellationToken token)
    {
        if (unexpected)
            Log("Unexpected behavior, recovering position.");

        int attempts = 0;
        int softBanAttempts = 0;
        while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            var screenID = await GetCurrentScreen(token).ConfigureAwait(false);
            if (screenID == CurrentScreen_Softban)
            {
                softBanAttempts++;
                if (softBanAttempts > 10)
                    await RestartGameSWSH(token).ConfigureAwait(false);
            }

            attempts++;
            if (attempts >= 15)
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            await Click(B, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
        }
    }

    private async Task ExitSeedCheckTrade(CancellationToken token)
    {
        // Seed Check Bot doesn't show anything, so it can skip the first B press.
        int attempts = 0;
        while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            attempts++;
            if (attempts >= 15)
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
        }

        await Task.Delay(3_000, token).ConfigureAwait(false);
    }

    private async Task ResetTradePosition(CancellationToken token)
    {
        Log("Resetting bot position.");

        // Shouldn't ever be used while not on overworld.
        if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            await ExitTrade(true, token).ConfigureAwait(false);

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

    private async Task<bool> CheckIfSearchingForLinkTradePartner(CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync(LinkTradeSearchingOffset, 1, token).ConfigureAwait(false);
        return data[0] == 1; // changes to 0 when found
    }

    private async Task<bool> CheckIfSearchingForSurprisePartner(CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync(SurpriseTradeSearchOffset, 8, token).ConfigureAwait(false);
        return BitConverter.ToUInt32(data, 0) == SurpriseTradeSearch_Searching;
    }

    private async Task<string> GetTradePartnerName(TradeMethod tradeMethod, CancellationToken token)
    {
        var ofs = GetTrainerNameOffset(tradeMethod);
        var data = await Connection.ReadBytesAsync(ofs, 26, token).ConfigureAwait(false);
        return StringConverter8.GetString(data);
    }

    private async Task<string> GetTradePartnerTID7(TradeMethod tradeMethod, CancellationToken token)
    {
        var ofs = GetTrainerTIDSIDOffset(tradeMethod);
        var data = await Connection.ReadBytesAsync(ofs, 8, token).ConfigureAwait(false);

        var tidsid = BitConverter.ToUInt32(data, 0);
        var tid7 = $"{tidsid % 1_000_000:000000}";
        return tid7;
    }

    public async Task<ulong> GetTradePartnerNID(CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync(LinkTradePartnerNIDOffset, 8, token).ConfigureAwait(false);
        return BitConverter.ToUInt64(data, 0);
    }
    private async Task<(PK8 toSend, PokeTradeResult check)> HandleFixOT(SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, PartnerDataHolder partner, CancellationToken token)
    {
        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, "Here's what you showed me!");

        var adOT = AbstractTrade<PK8>.HasAdName(offered, out _);
        var laInit = new LegalityAnalysis(offered);
        if (!adOT && laInit.Valid)
        {
            poke.SendNotification(this, "No ad detected in Nickname or OT, and the Pokémon is legal. Exiting trade.");
            return (offered, PokeTradeResult.TrainerRequestBad);
        }

        var clone = offered.Clone();
        if (Hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        string shiny = string.Empty;
        if (!AbstractTrade<PK8>.ShinyLockCheck(offered.Species, AbstractTrade<PK8>.FormOutput(offered.Species, offered.Form, out _), $"{(Ball)offered.Ball}"))
            shiny = $"\nShiny: {(offered.ShinyXor == 0 ? "Square" : offered.IsShiny ? "Star" : "No")}";
        else shiny = "\nShiny: No";

        var name = partner.TrainerName;
        var ball = $"\n{(Ball)offered.Ball}";
        var extraInfo = $"OT: {name}{ball}{shiny}";
        var set = ShowdownParsing.GetShowdownText(offered).Split('\n').ToList();
        var shinyRes = set.Find(x => x.Contains("Shiny"));
        if (shinyRes != null)
            set.Remove(shinyRes);
        set.InsertRange(1, extraInfo.Split('\n'));

        if (!laInit.Valid)
        {
            Log($"FixOT request has detected an illegal Pokémon from {name}: {(Species)offered.Species}");
            var report = laInit.Report();
            Log(laInit.Report());
            poke.SendNotification(this, $"**Shown Pokémon is not legal. Attempting to regenerate...**\n\n```{report}```");
            if (DumpSetting.Dump)
                DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);
        }

        if (clone.FatefulEncounter)
        {
            clone.SetDefaultNickname(laInit);
            var info = new SimpleTrainerInfo { Gender = clone.OriginalTrainerGender, Language = clone.Language, OT = name, TID16 = clone.TID16, SID16 = clone.SID16, Generation = 8 };
            var mg = EncounterEvent.GetAllEvents().Where(x => x.Species == clone.Species && x.Form == clone.Form && x.IsShiny == clone.IsShiny && x.OriginalTrainerName == clone.OriginalTrainerName).ToList();
            if (mg.Count > 0)
                clone = AbstractTrade<PK8>.CherishHandler(mg.First(), info);
            else clone = (PK8)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);
        }
        else clone = (PK8)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);

        clone = (PK8)AbstractTrade<PK8>.TrashBytes(clone, new LegalityAnalysis(clone));
        clone.ResetPartyStats();
        var la = new LegalityAnalysis(clone);
        if (!la.Valid)
        {
            poke.SendNotification(this, "This Pokémon is not legal per PKHeX's legality checks. I was unable to fix this. Exiting trade.");
            return (clone, PokeTradeResult.IllegalTrade);
        }

        poke.SendNotification(this, $"{(!laInit.Valid ? "**Legalized" : "**Fixed Nickname/OT for")} {(Species)clone.Species}**!");
        Log($"{(!laInit.Valid ? "Legalized" : "Fixed Nickname/OT for")} {(Species)clone.Species}!");

        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemon(clone, 0, 0, token, sav).ConfigureAwait(false);
        await Click(A, 0_500, token).ConfigureAwait(false);
        poke.SendNotification(this, "Now confirm the trade!");

        await Task.Delay(6_000, token).ConfigureAwait(false);
        var pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 1_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
        bool changed = pk2 == null || clone.Species != pk2.Species || offered.OriginalTrainerName != pk2.OriginalTrainerName;
        if (changed)
        {
            Log($"{name} changed the shown Pokémon ({(Species)clone.Species}){(pk2 != null ? $" to {(Species)pk2.Species}" : "")}");
            poke.SendNotification(this, "**Send away the originally shown Pokémon, please!**");
            var timer = 10_000;
            while (changed)
            {
                pk2 = await ReadUntilPresent(LinkTradePartnerPokemonOffset, 2_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
                changed = pk2 == null || clone.Species != pk2.Species || offered.OriginalTrainerName != pk2.OriginalTrainerName;
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timer -= 1_000;
                if (timer <= 0)
                    break;
            }
        }

        if (changed)
        {
            poke.SendNotification(this, "Pokémon was swapped and not changed back. Exiting trade.");
            Log("Trading partner did not wish to send away their ad-mon.");
            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        await Click(A, 0_500, token).ConfigureAwait(false);
        for (int i = 0; i < 5; i++)
            await Click(A, 0_500, token).ConfigureAwait(false);

        return (clone, PokeTradeResult.Success);
    }

    private async Task<bool> SetPkmWithSwappedIDDetails(PK8 toSend, string trainerName, SAV8SWSH sav, CancellationToken token)
    {
        var data = await Connection.ReadBytesAsync(LinkTradePartnerNameOffset - 0x8, 8, token).ConfigureAwait(false);
        var tidsid = BitConverter.ToUInt32(data, 0);
        var cln = (PK8)toSend.Clone();
        cln.OriginalTrainerGender = data[6];
        cln.TrainerTID7 = tidsid % 1_000_000;
        cln.TrainerSID7 = tidsid / 1_000_000;
        cln.Language = data[5];
        cln.OriginalTrainerName = trainerName;

        if (!toSend.IsNicknamed)
            cln.ClearNickname();

        if (toSend.IsShiny)
            cln.SetShiny();

        cln.RefreshChecksum();

        var tradeswsh = new LegalityAnalysis(cln);
        if (tradeswsh.Valid)
        {
            Log($"Pokemon is valid, use trade partnerInfo");
            await SetBoxPokemon(cln, 0, 0, token, sav).ConfigureAwait(false);
        }
        else
        {
            Log($"Pokemon not valid, do nothing to trade Pokemon");
        }

        return tradeswsh.Valid;
    }
}
