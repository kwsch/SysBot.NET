using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Pokemon.BasePokeDataOffsetsBS;
using static SysBot.Base.SwitchButton;
using Decoder = SysBot.Base.Decoder;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor8BS : PokeRoutineExecutor<PB8>
    {
        protected IPokeDataOffsetsBS Offsets { get; private set; } = new PokeDataOffsetsBS_BD();
        protected PokeRoutineExecutor8BS(PokeBotState cfg) : base(cfg)
        {
        }

        protected async Task<byte[]> PointerPeek(int size, IEnumerable<long> jumps, CancellationToken token)
        {
            byte[] command = Encoding.UTF8.GetBytes($"pointerPeek {size}{string.Concat(jumps.Select(z => $" {z}"))}\r\n");
            byte[] socketReturn = await SwitchConnection.ReadRaw(command, (size * 2) + 1, token).ConfigureAwait(false);
            return Decoder.ConvertHexByteStringToBytes(socketReturn);
        }

        // SysBot.NET likes heap offsets
        protected async Task<ulong> PointerRelative(IEnumerable<long> jumps, CancellationToken token)
        {
            byte[] command = Encoding.UTF8.GetBytes($"pointerRelative{string.Concat(jumps.Select(z => $" {z}"))}\r\n");
            byte[] socketReturn = await SwitchConnection.ReadRaw(command, (sizeof(ulong) * 2) + 1, token).ConfigureAwait(false);
            var bytes = Decoder.ConvertHexByteStringToBytes(socketReturn);
            bytes = bytes.Reverse().ToArray();

            var offset = BitConverter.ToUInt64(bytes, 0);
            return offset;
        }

        protected async Task PointerPoke(byte[] bytes, IEnumerable<long> jumps, CancellationToken token)
        {
            byte[] command = Encoding.UTF8.GetBytes($"pointerPoke 0x{string.Concat(bytes.Select(z => $"{z:X2}"))}{string.Concat(jumps.Select(z => $" {z}"))}\r\n");
            await SwitchConnection.SendRaw(command, token).ConfigureAwait(false);
        }

        public override async Task<PB8> ReadPokemon(ulong offset, CancellationToken token) => await ReadPokemon(offset, BoxFormatSlotSize, token).ConfigureAwait(false);

        public override async Task<PB8> ReadPokemon(ulong offset, int size, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
            return new PB8(data);
        }

        public override async Task<PB8> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token)
        {
            var (valid, offset) = await ValidatePointerAll(jumps, token).ConfigureAwait(false);
            if (!valid)
                return new PB8();
            return await ReadPokemon(offset, token).ConfigureAwait(false);
        }

        public async Task<bool> ReadIsChanged(uint offset, byte[] original, CancellationToken token)
        {
            var result = await Connection.ReadBytesAsync(offset, original.Length, token).ConfigureAwait(false);
            return !result.SequenceEqual(original);
        }

        public override async Task<PB8> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            if (box != 0 || (uint)slot > 5)
                throw new Exception("I can only see b1s1 to b1s5 for now");

            var jumps = Offsets.BoxStartPokemonPointer.ToArray();
            jumps[^1] -= slot * BoxFormatSlotSize;
            return await ReadPokemonPointer(jumps, BoxFormatSlotSize, token).ConfigureAwait(false);
        }

        public async Task SetBoxPokemon(PB8 pkm, CancellationToken token, ITrainerInfo? sav = null)
        {
            if (sav != null)
            {
                // Update PKM to the current save's handler data
                DateTime Date = DateTime.Now;
                pkm.Trade(sav, Date.Day, Date.Month, Date.Year);
                pkm.RefreshChecksum();
            }

            pkm.ResetPartyStats();
            await PointerPoke(pkm.EncryptedPartyData, Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
        }

        public async Task<SAV8BS> IdentifyTrainer(CancellationToken token)
        {
            // pull title so we know which set of offsets to use
            string title = Encoding.ASCII.GetString(await SwitchConnection.ReadRaw(SwitchCommand.GetTitleID(), 17, token).ConfigureAwait(false)).Trim();
            Offsets = title switch
            {
                BrilliantDiamondID => new PokeDataOffsetsBS_BD(),
                ShiningPearlID => new PokeDataOffsetsBS_SP(),
                _ => throw new Exception($"Title for {title} is unknown."),
            };

            // generate a fake savefile
            var myStatusOffset = await PointerAll(Offsets.MainSavePointer, token).ConfigureAwait(false);

            // we only need config and mystatus regions
            const ulong offs = 0x79B74;
            var savMyStatus = await SwitchConnection.ReadBytesAbsoluteAsync(myStatusOffset + offs, 0x40 + 0x50, token).ConfigureAwait(false);
            var bytes = new byte[offs].Concat(savMyStatus).ToArray();

            var sav = new SAV8BS(bytes);
            InitSaveData(sav);

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

            Log("Setting BDSP-specific hid waits.");
            await Connection.SendAsync(SwitchCommand.Configure(SwitchConfigureParameter.keySleepTime, 50), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Configure(SwitchConfigureParameter.pollRate, 50), token).ConfigureAwait(false);
        }

        public async Task CleanExit(IBotStateSettings settings, CancellationToken token)
        {
            if (settings.ScreenOff)
            {
                Log("Turning on screen.");
                await SetScreen(ScreenState.On, token).ConfigureAwait(false);
            }
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

        public async Task Unban(CancellationToken token)
        {
            Log("Soft ban detected, unbanning.");
            // Write the float value to 0.
            var data = BitConverter.GetBytes(0);
            await PointerPoke(data, Offsets.UnionWorkPenaltyPointer, token).ConfigureAwait(false);
        }

        public async Task<bool> CheckIfSoftBanned(ulong offset, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 4, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0) != 0;
        }

        public async Task CloseGame(PokeTradeHubConfig config, CancellationToken token)
        {
            var timing = config.Timings;
            // Close out of the game
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

            // Menus here can go in the order: Update Prompt -> Profile -> DLC check -> Unable to use DLC.
            //  The user can optionally turn on the setting if they know of a breaking system update incoming.
            if (timing.AvoidSystemUpdate)
            {
                await Click(DUP, 0_600, token).ConfigureAwait(false);
                await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);
            }

            await Click(A, 1_000 + timing.ExtraTimeCheckDLC, token).ConfigureAwait(false);
            // If they have DLC on the system and can't use it, requires an UP + A to start the game.
            // Should be harmless otherwise since they'll be in loading screen.
            await Click(DUP, 0_600, token).ConfigureAwait(false);
            await Click(A, 0_600, token).ConfigureAwait(false);

            Log("Restarting the game!");

            // Switch Logo lag, skip cutscene, game load screen
            await Task.Delay(22_000 + timing.ExtraTimeLoadGame, token).ConfigureAwait(false);

            for (int i = 0; i < 10; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);

            var timer = 60_000;
            while (!await IsSceneID(SceneID_Field, token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timer -= 1_000;
                // We haven't made it back to overworld after a minute, so press A every 6 seconds hoping to restart the game.
                // Don't risk it if hub is set to avoid updates.
                if (timer <= 0 && !timing.AvoidSystemUpdate)
                {
                    Log("Still not in the game, initiating rescue protocol!");
                    while (!await IsSceneID(SceneID_Field, token).ConfigureAwait(false))
                        await Click(A, 6_000, token).ConfigureAwait(false);
                    break;
                }
            }

            await Task.Delay(5_000, token).ConfigureAwait(false);
            Log("Back in the overworld!");
        }

        private async Task<uint> GetSceneID(CancellationToken token)
        {
            var xVal = await PointerPeek(1, Offsets.SceneIDPointer, token).ConfigureAwait(false);
            var xParsed = BitConverter.ToUInt32(xVal, 0);
            return xParsed;
        }

        private async Task<bool> IsSceneID(uint expected, CancellationToken token)
        {
            var byt = await PointerPeek(1, Offsets.SceneIDPointer, token).ConfigureAwait(false);
            return byt[0] == expected;
        }

        // Uses absolute offset which is set each session. Checks for IsGaming or IsTalking.
        public async Task<bool> IsUnionWork(ulong offset, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        // Whenever we're in a trade, this pointer will be loaded, otherwise 0
        public async Task<bool> IsPartnerParamLoaded(CancellationToken token)
        {
            var byt = await PointerPeek(8, Offsets.LinkTradePartnerParamPointer, token).ConfigureAwait(false);
            return BitConverter.ToUInt64(byt, 0) != 0;
        }

        public async Task<ulong> GetTradePartnerNID(CancellationToken token) => BitConverter.ToUInt64(await PointerPeek(sizeof(ulong), Offsets.LinkTradePartnerNIDPointer, token).ConfigureAwait(false), 0);
    }
}
