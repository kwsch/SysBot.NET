using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsLZA;

namespace SysBot.Pokemon;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class PokeTradeBotLZA(PokeTradeHub<PA9> Hub, PokeBotState Config) : PokeRoutineExecutor9LZA(Config), ICountBot
{
    private readonly TradeSettings TradeSettings = Hub.Config.Trade;
    private readonly TradeAbuseSettings AbuseSettings = Hub.Config.TradeAbuse;

    public ICountSettings Counts => TradeSettings;

    /// <summary>
    /// Folder to dump received trade data to.
    /// </summary>
    /// <remarks>If null, will skip dumping.</remarks>
    private readonly IDumper DumpSetting = Hub.Config.Folder;

    /// <summary>
    /// Synchronized start for multiple bots.
    /// </summary>
    public bool ShouldWaitAtBarrier { get; private set; }

    /// <summary>
    /// Tracks failed synchronized starts to attempt to re-sync.
    /// </summary>
    public int FailedBarrier { get; private set; }

    // Cached offsets that stay the same per session.
    private ulong BoxStartOffset;

    // Cached offsets that stay the same after connecting online.
    private ulong TradePartnerNIDOffset;
    private ulong TradePartnerTIDOffset;
    private ulong TradePartnerOTOffset;

    // Cached offsets that stay the same per trade.
    private ulong TradePartnerStatusOffset;

    // Stores whether we returned all the way to the overworld, which repositions the cursor.
    private bool StartFromOverworld = true;

    public override async Task MainLoop(CancellationToken token)
    {
        try
        {
            await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

            Log("Identifying trainer data of the host console.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            RecentTrainerCache.SetRecentTrainer(sav);
            await InitializeSessionOffsets(token).ConfigureAwait(false);
            // It's possible to start off already connected.
            if (await IsConnected(token).ConfigureAwait(false))
                await InitializeOnlineOffsets(token).ConfigureAwait(false);
            StartFromOverworld = true;

            Log($"Starting main {nameof(PokeTradeBotLZA)} loop.");
            await InnerLoop(sav, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log(e.Message);
        }

        Log($"Ending {nameof(PokeTradeBotLZA)} loop.");
        await HardStop().ConfigureAwait(false);
    }

    public override Task HardStop()
    {
        UpdateBarrier(false);
        return CleanExit(CancellationToken.None);
    }

    private async Task InnerLoop(SAV9ZA sav, CancellationToken token)
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

    private async Task DoTrades(SAV9ZA sav, CancellationToken token)
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

    private Task WaitForQueueStep(int waitCounter, CancellationToken token)
    {
        if (waitCounter == 0)
        {
            // Updates the assets.
            Hub.Config.Stream.IdleAssets(this);
            Log("Nothing to check, waiting for new users...");
        }

        const int interval = 10;
        if (waitCounter % interval == interval - 1 && Hub.Config.AntiIdle)
            return Click(B, 1_000, token);
        return Task.Delay(1_000, token);
    }

    protected virtual (PokeTradeDetail<PA9>? detail, uint priority) GetTradeData(PokeRoutineType type)
    {
        if (Hub.Queues.TryDequeue(type, out var detail, out var priority))
            return (detail, priority);
        if (Hub.Queues.TryDequeueLedy(out detail))
            return (detail, PokeTradePriorities.TierFree);
        return (null, PokeTradePriorities.TierFree);
    }

    private async Task PerformTrade(SAV9ZA sav, PokeTradeDetail<PA9> detail, PokeRoutineType type, uint priority, CancellationToken token)
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

    private void HandleAbortedTrade(PokeTradeDetail<PA9> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
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

    private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV9ZA sav, PokeTradeDetail<PA9> poke, CancellationToken token)
    {
        // Update Barrier Settings
        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);
        Hub.Config.Stream.EndEnterCode(this);

        // If we're expected to be on the overworld and we aren't, recover there.
        if (StartFromOverworld && !await IsOnOverworld(token).ConfigureAwait(false))
            await ResetToOverworld(token).ConfigureAwait(false);

        // If we're expected to start on Link Play menu and we aren't there, reset to overworld.
        if (!StartFromOverworld && !await IsOnMenu(MenuState.LinkPlay, token).ConfigureAwait(false))
        {
            await ResetToOverworld(token).ConfigureAwait(false);
            StartFromOverworld = true;
        }

        var toSend = poke.TradeData;
        if (toSend.Species != 0)
            await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);

        // If we're starting from the overworld, open the menu.
        if (StartFromOverworld)
        {
            Log("Entering Link Play menu.");
            await Click(X, 0_800, token).ConfigureAwait(false);
            await Click(DUP, 0_300, token).ConfigureAwait(false);
            await Click(A, 0_800, token).ConfigureAwait(false);
        }

        Log("Selecting Link Trade.");
        await Click(DLEFT, 0_400, token).ConfigureAwait(false);
        await Click(A, 0_800, token).ConfigureAwait(false);
        await Click(DRIGHT, 0_400, token).ConfigureAwait(false);

        // If we're not connected, the first click will connect and save the game, which will take a few seconds.
        if (!await IsConnected(token).ConfigureAwait(false))
        {
            Log("Connecting online.");
            await Click(A, 0_300, token).ConfigureAwait(false);
            while (!await IsConnected(token).ConfigureAwait(false))
                await (Click(A, 0_200, token)).ConfigureAwait(false);
            await (Task.Delay(1_000, token)).ConfigureAwait(false);
            Log("Successfully connected!");
            await InitializeOnlineOffsets(token).ConfigureAwait(false);
        }
        else
        {
            // If already connected, an extra click is needed to open the keypad.
            await Click(A, 0_500, token).ConfigureAwait(false);
        }

        // Only need one more click to open the keypad.
        await Click(A, 0_800, token).ConfigureAwait(false);

        // Loading code entry.
        if (poke.Type != PokeTradeType.Random)
            Hub.Config.Stream.StartEnterCode(this);
        await Task.Delay(Hub.Config.Timings.ExtraTimeOpenCodeEntry, token).ConfigureAwait(false);

        var code = poke.Code;

        // LZA has more complex logic for entering the link code.
        await EnterLinkCodeLZA(code, token).ConfigureAwait(false);

        // Wait for Barrier to trigger all bots simultaneously.
        WaitAtBarrierIfApplicable(token);
        await Click(PLUS, 1_000, token).ConfigureAwait(false);

        poke.TradeSearching(this);

        // Wait for a Trainer...
        var partnerFound = await WaitForTradePartner(token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
        {
            await ResetToOverworld(token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }
        if (!partnerFound)
        {
            // Make sure we cancel the trade search first if we waited less than 55 seconds.
            // Actual timeout seems to be just over 60 seconds, but it's better not to accidentally click into keypad again.
            if (Hub.Config.Trade.TradeWaitTime < 55)
            {
                await Click(B, 0_500, token).ConfigureAwait(false);
                await Click(A, 0_300, token).ConfigureAwait(false);
            }
            await ResetToLinkPlay( token).ConfigureAwait(false);
            return PokeTradeResult.NoTrainerFound;
        }

        Hub.Config.Stream.EndEnterCode(this);

        // Some more time to fully enter the trade.
        await Task.Delay(1_000 + Hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false);

        var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
        RecordUtil<PokeTradeBotLZA>.Record($"Initiating\t{tradePartner.NID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
        Log($"Found Link Trade partner: {tradePartner.TrainerName}-{tradePartner.TID7} (ID: {tradePartner.NID})");

        var partnerCheck = await CheckPartnerReputation(this, poke, tradePartner.NID, tradePartner.TrainerName, AbuseSettings, token);
        if (partnerCheck != PokeTradeResult.Success)
        {
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return partnerCheck;
        }

        poke.SendNotification(this, $"Found Link Trade partner: {tradePartner.TrainerName}. Waiting for a Pokémon...");

        if (poke.Type == PokeTradeType.Dump)
        {
            var result = await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return result;
        }

        // Watch their status to indicate they have offered a Pokémon as well.
        var offering = await ReadUntilChanged(TradePartnerStatusOffset, [0x3], 25_000, 1_000, true, true, token).ConfigureAwait(false);
        if (!offering)
        {
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        Log("Checking offered Pokémon.");
        // If we got to here, we can read their offered Pokémon.

        // Wait for user input... Needs to be different from the previously offered Pokémon.
        var offered = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
        if (offered == null || offered.Species == 0 || !offered.ChecksumValid)
        {
            Log("Trade ended because trainer offer was rescinded too quickly.");
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return PokeTradeResult.TrainerOfferCanceledQuick;
        }
        offered.Heal();
        offered.RefreshChecksum();

        var trainer = new PartnerDataHolder(0, tradePartner.TrainerName, tradePartner.TID7);
        (toSend, PokeTradeResult update) = await GetEntityToSend(sav, poke, offered, toSend, trainer, token).ConfigureAwait(false);
        if (update != PokeTradeResult.Success)
        {
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return update;
        }

        if (Hub.Config.Trade.DisallowTradeEvolve && TradeEvolutions.WillTradeEvolve(offered.Species, offered.Form, offered.HeldItem, toSend.Species))
        {
            Log("Trade cancelled because trainer offered a Pokémon that would evolve upon trade.");
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return PokeTradeResult.TradeEvolveNotAllowed;
        }

        Log("Confirming trade.");
        var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
        if (tradeResult != PokeTradeResult.Success)
        {
            if (tradeResult == PokeTradeResult.TrainerLeft)
                Log("Trade canceled because trainer left the trade.");
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return tradeResult;
        }

        if (token.IsCancellationRequested)
        {
            await ResetToOverworld(token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }

        // Trade was successful!
        var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
        // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
        if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
        {
            Log("User did not complete the trade.");
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        // As long as we got rid of our inject in b1s1, assume the trade went through.
        Log("User completed the trade.");
        poke.TradeFinished(this, received);

        // Only log if we completed the trade.
        UpdateCountsAndExport(poke, received, toSend);

        // Log for Trade Abuse tracking.
        LogSuccessfulTrades(poke, tradePartner.NID, tradePartner.TrainerName);

        await ResetToLinkPlay(token).ConfigureAwait(false);
        return PokeTradeResult.Success;
    }

    private void UpdateCountsAndExport(PokeTradeDetail<PA9> poke, PA9 received, PA9 toSend)
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

    private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PA9> detail, CancellationToken token)
    {
        // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
        var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

        await Click(A, 3_000, token).ConfigureAwait(false);
        for (int i = 0; i < Hub.Config.Trade.MaxTradeConfirmTime; i++)
        {
            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
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
        if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
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
            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
            {
                await Task.Delay(0_100, token).ConfigureAwait(false);
                ctr -= 0_100;
                continue;
            }
            await Task.Delay(0_500, token).ConfigureAwait(false);

            // If we made it to here, then we're in the box. Set the offset for their status.
            var (valid, offset) = await ValidatePointerAll(Offsets.TradePartnerStatusPointer, token).ConfigureAwait(false);
            if (!valid)
                continue;
            TradePartnerStatusOffset = offset;
            return true;
        }
        return false;
    }

    // Generally used for recovery if we can't make it to Link Play for some reason.
    private async Task ResetToOverworld(CancellationToken token)
    {
        if (await IsOnOverworld(token).ConfigureAwait(false))
            return;

        Log("Resetting to the overworld...");
        // If we're in the Box or searching for a Link Trade, we need to use the BAB approach, otherwise we can just mash B.
        var ctr = 120_000;
        while (await GetMenuState(token).ConfigureAwait(false) >= MenuState.LinkTrade)
        {
            if (ctr < 0)
            {
                // Failed to exist somehow.
                await RestartGameLZA(token).ConfigureAwait(false);
                return;
            }

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) < MenuState.LinkTrade)
                break;

            var box = await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false);
            await Click(box ? A : B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) < MenuState.LinkTrade)
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) < MenuState.LinkTrade)
                break;
            ctr -= 3_000;
        }

        // From here, we should be able to press B.
        while (!await IsOnOverworld(token).ConfigureAwait(false))
            await Click(B, 0_200, token).ConfigureAwait(false);

        StartFromOverworld = true;
    }

    // We'll be doing this most of the time. Going to the overworld is a little slower.
    private async Task ResetToLinkPlay(CancellationToken token)
    {
        var current = await GetMenuState(token).ConfigureAwait(false);
        if (current == MenuState.LinkPlay)
            return;

        // Already on an earlier menu than Link Trade. Just go to overworld and start over next trade.
        if (current < MenuState.LinkPlay)
        {
            await ResetToOverworld(token).ConfigureAwait(false);
            StartFromOverworld = true;
            return;
        }

        Log("Resetting to the Link Play menu...");
        // If we're in the Box or searching for a Link Trade, we need to use the BAB approach, otherwise we can just mash B.
        var ctr = 120_000;
        while (await GetMenuState(token).ConfigureAwait(false) >= MenuState.LinkPlay)
        {
            if (ctr < 0)
            {
                // Failed to exist somehow.
                await RestartGameLZA(token).ConfigureAwait(false);
                StartFromOverworld = true;
                return;
            }

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) == MenuState.LinkPlay)
            {
                StartFromOverworld = false;
                return;
            }

            var box = await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false);
            await Click(box ? A : B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) == MenuState.LinkPlay)
            {
                StartFromOverworld = false;
                return;
            }

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) == MenuState.LinkPlay)
            {
                StartFromOverworld = false;
                return;
            }
            ctr -= 3_000;
        }
    }

    // LZA saves the previous Link Code after the first trade.
    // If the pointer isn't valid, we haven't traded yet.
    // Otherwise, we should be able to see if it's the same and how long it is.
    private async Task EnterLinkCodeLZA(int code, CancellationToken token)
    {
        var (valid, _) = await ValidatePointerAll(Offsets.LinkTradeCodePointer, token).ConfigureAwait(false);
        if (!valid)
        {
            // If it's not valid, then we can freely enter our code in because no trades have been done yet.
            Log($"Entering Link Trade code: {code:0000 0000}...");
            await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);
        }
        else
        {
            var prev_code = await GetStoredLinkTradeCode(token).ConfigureAwait(false);
            if (prev_code != code) // Only clear if the new code is different.
            {
                var code_length = await GetStoredLinkTradeCodeLength(token).ConfigureAwait(false);
                if (code_length > 0)
                    await PressAndHold(B, (code_length * 0_100) + 0_200, 0_100, token).ConfigureAwait(false);

                Log($"Entering Link Trade code: {code:0000 0000}...");
                await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);
            }
            else
            {
                Log($"Using previous Link Trade code: {code:0000 0000}.");
            }
        }
    }

    // These don't change per game session, and we access them frequently, so set these each time we start.
    private async Task InitializeSessionOffsets(CancellationToken token)
    {
        Log("Caching session offsets...");
        BoxStartOffset = await SwitchConnection.PointerAll(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
    }

    // These don't change per online session, so set them whenever we connect.
    private async Task InitializeOnlineOffsets(CancellationToken token)
    {
        Log("Caching online offsets...");
        var baseOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerDataPointer, token).ConfigureAwait(false);
        TradePartnerNIDOffset = baseOffset + TradePartnerNIDShift;
        TradePartnerTIDOffset = baseOffset + TradePartnerTIDShift;
        TradePartnerOTOffset = baseOffset + TradePartnerOTShift;
    }

    // todo: future
    protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PA9> detail, CancellationToken token)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return false;
    }

    private async Task RestartGameLZA(CancellationToken token)
    {
        //await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
        //await InitializeSessionOffsets(token).ConfigureAwait(false);
    }

    private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PA9> detail, CancellationToken token)
    {
        int ctr = 0;
        var time = TimeSpan.FromSeconds(Hub.Config.Trade.MaxDumpTradeTime);
        var start = DateTime.Now;

        var pkprev = new PA9();
        var bctr = 0;
        while (ctr < Hub.Config.Trade.MaxDumpsPerTrade && DateTime.Now - start < time)
        {
            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
                break;
            if (bctr++ % 3 == 0)
                await Click(B, 0_100, token).ConfigureAwait(false);

            // Wait for user input... Needs to be different from the previously offered Pokémon.
            var pk = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (pk == null || pk.Species < 1 || !pk.ChecksumValid || SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
                continue;
            pk.Heal();
            pk.RefreshChecksum();

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
            var msg = Hub.Config.Trade.DumpTradeLegalityCheck ? verbose : $"File {ctr}";

            // Extra information about trainer data for people requesting with their own trainer data.
            var ot = pk.OriginalTrainerName;
            var ot_gender = pk.OriginalTrainerGender == 0 ? "Male" : "Female";
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
        detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank PA9
        return PokeTradeResult.Success;
    }

    private async Task<TradePartnerLZA> GetTradePartnerInfo(CancellationToken token)
    {
        // Grab a chunk of bytes starting from the NID. Most likely this will also include the OT and TID.
        // Check if data is loaded at the last byte of this chunk. If it's not loaded, we'll have to try and find OT and TID at the fallback location.
        var chunk = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerNIDOffset, 0x69, token).ConfigureAwait(false);

        // NID should be the first 8 bytes, converted to a ulong.
        var id = chunk.AsSpan(0, 8).ToArray();
        var nid = BitConverter.ToUInt64(id);
        if (nid == 0) // They probably left too quickly, so try the backup pointer.
            nid = await GetTradePartnerNID(token).ConfigureAwait(false);

        // Now check if the last byte is populated.
        if (chunk[0x68] != 0)
        {
            // Data is loaded here, so we can read TID and OT from here.
            var tid = chunk.AsSpan(0x44, 4).ToArray();
            var name = chunk.AsSpan(0x4C, TradePartnerLZA.MaxByteLengthStringObject).ToArray();
            return new TradePartnerLZA(nid, tid, name);
        }
        // Data is not loaded at the expected place, so we have to read TID and OT from the fallback location.
        {
            chunk = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerTIDOffset + FallBackTradePartnerDataShift, 34, token).ConfigureAwait(false);
            var tid = chunk.AsSpan(0, 4).ToArray();
            var name = chunk.AsSpan(0x8, TradePartnerLZA.MaxByteLengthStringObject).ToArray();
            return new TradePartnerLZA(nid, tid, name);
        }
    }

    protected virtual async Task<(PA9 toSend, PokeTradeResult check)> GetEntityToSend(SAV9ZA sav, PokeTradeDetail<PA9> poke, PA9 offered, PA9 toSend, PartnerDataHolder partnerID, CancellationToken token)
    {
        return poke.Type switch
        {
            PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
            PokeTradeType.Clone => await HandleClone(sav, poke, offered, token).ConfigureAwait(false),
            _ => (toSend, PokeTradeResult.Success),
        };
    }

    private async Task<(PA9 toSend, PokeTradeResult check)> HandleClone(SAV9ZA sav, PokeTradeDetail<PA9> poke, PA9 offered, CancellationToken token)
    {
        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, "Here's what you showed me!");

        var la = new LegalityAnalysis(offered);
        if (!la.Valid)
        {
            Log($"Clone request (from {poke.Trainer.TrainerName}) has detected an invalid Pokémon: {GameInfo.GetStrings("en").Species[offered.Species]}.");
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

        poke.SendNotification(this, $"**Cloned your {GameInfo.GetStrings("en").Species[clone.Species]}!**\nNow press B to cancel your offer and trade me a Pokémon you don't want.");
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
        var hovering = await ReadUntilChanged(TradePartnerStatusOffset, [0x2], 25_000, 1_000, true, true, token).ConfigureAwait(false);
        if (!hovering)
        {
            Log("Trade partner did not change their initial offer.");
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return false;
        }
        var offering = await ReadUntilChanged(TradePartnerStatusOffset, [0x3], 25_000, 1_000, true, true, token).ConfigureAwait(false);
        if (!offering)
        {
            await ResetToLinkPlay(token).ConfigureAwait(false);
            return false;
        }
        return true;
    }

    private async Task<(PA9 toSend, PokeTradeResult check)> HandleRandomLedy(SAV9ZA sav, PokeTradeDetail<PA9> poke, PA9 offered, PA9 toSend, PartnerDataHolder partner, CancellationToken token)
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
            var nickname = offered.IsNicknamed ? $" (Nickname: \"{offered.Nickname}\")" : string.Empty;
            poke.SendNotification(this, $"No match found for the offered {GameInfo.GetStrings("en").Species[offered.Species]}{nickname}.");
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
