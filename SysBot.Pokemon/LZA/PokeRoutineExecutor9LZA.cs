using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsLZA;

namespace SysBot.Pokemon;

public abstract class PokeRoutineExecutor9LZA(PokeBotState Config) : PokeRoutineExecutor<PA9>(Config)
{
    protected PokeDataOffsetsLZA Offsets { get; } = new();

    public override Task<PA9> ReadPokemon(ulong offset, CancellationToken token) => ReadPokemon(offset, BoxFormatSlotSize, token);

    public override async Task<PA9> ReadPokemon(ulong offset, int size, CancellationToken token)
    {
        var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
        return new PA9(data);
    }

    public override async Task<PA9> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token)
    {
        var (valid, offset) = await ValidatePointerAll(jumps, token).ConfigureAwait(false);
        if (!valid)
            return new PA9();
        return await ReadPokemon(offset, token).ConfigureAwait(false);
    }

    public async Task<bool> ReadIsChanged(uint offset, byte[] original, CancellationToken token)
    {
        var result = await Connection.ReadBytesAsync(offset, original.Length, token).ConfigureAwait(false);
        return !result.SequenceEqual(original);
    }

    public override Task<PA9> ReadBoxPokemon(int box, int slot, CancellationToken token)
    {
        // Shouldn't be reading anything but box1slot1 here. Slots are not consecutive.
        var jumps = Offsets.BoxStartPokemonPointer.ToArray();
        return ReadPokemonPointer(jumps, BoxFormatSlotSize, token);
    }

    public Task SetBoxPokemonAbsolute(ulong offset, PA9 pkm, CancellationToken token, ITrainerInfo? sav = null)
    {
        if (sav != null)
        {
            // Update PKM to the current save's handler data
            pkm.UpdateHandler(sav);
        }

        pkm.Heal();
        pkm.RefreshChecksum();
        return SwitchConnection.WriteBytesAbsoluteAsync(pkm.EncryptedPartyData, offset, token);
    }

    public Task SetCurrentBox(byte box, CancellationToken token)
    {
        return SwitchConnection.PointerPoke([box], Offsets.CurrentBoxPointer, token);
    }

    public async Task<byte> GetCurrentBox(CancellationToken token)
    {
        var data = await SwitchConnection.PointerPeek(1, Offsets.CurrentBoxPointer, token).ConfigureAwait(false);
        return data[0];
    }

    public async Task<SAV9ZA> IdentifyTrainer(CancellationToken token)
    {
        // Check if botbase is on the correct version or later.
        await VerifyBotbaseVersion(token).ConfigureAwait(false);

        // Check title so we can warn if mode is incorrect.
        string title = await SwitchConnection.GetTitleID(token).ConfigureAwait(false);
        if (title != LegendsZAID)
            throw new Exception($"{title} is not a valid Pokémon Legends: Arceus title. Is your mode correct?");

        // Verify the game version.
        var game_version = await SwitchConnection.GetGameInfo("version", token).ConfigureAwait(false);
        if (!game_version.SequenceEqual(LZAGameVersion))
            throw new Exception($"Game version is not supported. Expected version {LZAGameVersion}, and current game version is {game_version}.");

        var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
        InitSaveData(sav);

        if (!IsValidTrainerData())
        {
            await CheckForRAMShiftingApps(token).ConfigureAwait(false);
            throw new Exception("Refer to the SysBot.NET wiki (https://github.com/kwsch/SysBot.NET/wiki/Troubleshooting) for more information.");
        }

        if (await GetTextSpeed(token).ConfigureAwait(false) < TextSpeedOption.Fast)
            throw new Exception("Text speed should be set to FAST. Fix this for correct operation.");

        return sav;
    }

    public async Task<SAV9ZA> GetFakeTrainerSAV(CancellationToken token)
    {
        var sav = new SAV9ZA();
        var info = sav.MyStatus;
        var read = await SwitchConnection.PointerPeek(info.Data.Length, Offsets.MyStatusPointer, token).ConfigureAwait(false);
        read.CopyTo(info.Data);
        return sav;
    }

    public async Task InitializeHardware(IBotStateSettings settings, CancellationToken token)
    {
        Log("Detaching on startup.");
        await DetachController(token).ConfigureAwait(false);
        if (settings.ScreenOff)
        {
            Log("Turning off screen.");
            await SetScreen(ScreenState.Off, token).ConfigureAwait(false);
        }
    }

    public async Task CleanExit(CancellationToken token)
    {
        await SetScreen(ScreenState.On, token).ConfigureAwait(false);
        Log("Detaching controllers on routine exit.");
        await DetachController(token).ConfigureAwait(false);
    }

    protected virtual async Task EnterLinkCode(int code, PokeTradeHubConfig config, CancellationToken token)
    {
        // Default implementation to just press directional arrows. Can do via Hid keys, but users are slower than bots at even the default code entry.
        var keys = TradeUtil.GetPresses(code);
        foreach (var key in keys)
        {
            int delay = config.Timings.KeypressTime;
            await Click(key, delay, token).ConfigureAwait(false);
        }
        // Confirm Code outside of this method (allow synchronization)
    }

    public async Task ReOpenGame(PokeTradeHubConfig config, CancellationToken token)
    {
        Log("Error detected, restarting the game!!");
        await CloseGame(config, token).ConfigureAwait(false);
        await StartGame(config, token).ConfigureAwait(false);
    }

    public async Task CloseGame(PokeTradeHubConfig config, CancellationToken token)
    {
        var timing = config.Timings;
        // Close out of the game
        await Click(B, 0_500, token).ConfigureAwait(false);
        await Click(HOME, 2_000 + timing.ExtraTimeReturnHome, token).ConfigureAwait(false);
        await Click(X, 1_000, token).ConfigureAwait(false);
        await Click(A, 5_000 + timing.ExtraTimeCloseGame, token).ConfigureAwait(false);
        Log("Closed out of the game!");
    }

    public async Task StartGame(PokeTradeHubConfig config, CancellationToken token)
    {
        var timing = config.Timings;
        // Open game.
        await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);

        // Menus here can go in the order: Update Prompt -> Profile -> Starts Game
        // The user can optionally turn on the setting if they know of a breaking system update incoming.
        if (timing.AvoidSystemUpdate)
        {
            await Task.Delay(1_000, token).ConfigureAwait(false); // Reduce the chance of misclicking here.
            await Click(DUP, 0_600, token).ConfigureAwait(false);
            await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);
        }

        await Click(A, 0_600, token).ConfigureAwait(false);

        Log("Restarting the game!");

        // Switch Logo and game load screen
        await Task.Delay(12_000 + timing.ExtraTimeLoadGame, token).ConfigureAwait(false);

        for (int i = 0; i < 8; i++)
            await Click(A, 1_000, token).ConfigureAwait(false);

        var timer = 60_000;
        while (!await IsOnOverworld(token).ConfigureAwait(false))
        {
            await Task.Delay(1_000, token).ConfigureAwait(false);
            timer -= 1_000;
            // We haven't made it back to overworld after a minute, so press A every 6 seconds hoping to restart the game.
            // Don't risk it if hub is set to avoid updates.
            if (timer <= 0 && !timing.AvoidSystemUpdate)
            {
                Log("Still not in the game, initiating rescue protocol!");
                while (!await IsOnOverworld(token).ConfigureAwait(false))
                    await Click(A, 6_000, token).ConfigureAwait(false);
                break;
            }
        }

        await Task.Delay(timing.ExtraTimeLoadOverworld, token).ConfigureAwait(false);
        Log("Back in the overworld!");
    }

    public async Task<ulong> GetTradePartnerNID(CancellationToken token)
    {
        var data = await SwitchConnection.PointerPeek(8, Offsets.TradePartnerBackupNIDPointer, token).ConfigureAwait(false);
        return BitConverter.ToUInt64(data, 0);
    }

    public async Task<bool> IsOnOverworld(CancellationToken token)
    {
        var data = await SwitchConnection.ReadBytesMainAsync(OverworldOffset, 1, token).ConfigureAwait(false);
        return data[0] == 1;
    }

    public async Task<TextSpeedOption> GetTextSpeed(CancellationToken token)
    {
        var data = await SwitchConnection.PointerPeek(1, Offsets.TextSpeedPointer, token).ConfigureAwait(false);
        return (TextSpeedOption)((data[0] & 7) >> 1);
    }

    public async Task<bool> IsConnected(CancellationToken token)
    {
        var data = await SwitchConnection.ReadBytesMainAsync(ConnectedOffset, 1, token).ConfigureAwait(false);
        return data[0] == 1;
    }

    public async Task<byte> GetStoredLinkTradeCodeLength(CancellationToken token)
    {
        var data = await SwitchConnection.PointerPeek(1, Offsets.LinkTradeCodeLengthPointer, token).ConfigureAwait(false);
        return data[0];
    }

    public async Task<int> GetStoredLinkTradeCode(CancellationToken token)
    {
        var data = await SwitchConnection.PointerPeek(16, Offsets.LinkTradeCodePointer, token).ConfigureAwait(false);
        var raw = StringConverter8.GetString(data);

        // Trim nulls and whitespace
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
            return 0;

        if (int.TryParse(trimmed, out int value))
            return value;

        return -1;
    }

    public async Task<bool> IsOnMenu(MenuState state, CancellationToken token)
    {
        var data = await SwitchConnection.ReadBytesMainAsync(MenuOffset, 1, token).ConfigureAwait(false);
        return (MenuState)data[0] == state;
    }

    public async Task<MenuState> GetMenuState(CancellationToken token)
    {
        var data = await SwitchConnection.ReadBytesMainAsync(MenuOffset, 1, token).ConfigureAwait(false);
        return (MenuState)data[0];
    }

    public enum MenuState : byte
    {
        Overworld = 0,
        XMenu = 1,
        LinkPlay = 2,
        LinkTrade = 3,
        InBox = 4,
    }
}
