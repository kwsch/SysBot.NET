using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Pokemon.PokeDataOffsetsLGPE;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutorLGPE : PokeRoutineExecutor<PB7>
    {
        protected PokeRoutineExecutorLGPE(PokeBotState cfg) : base(cfg) { }

        public LanguageID GameLang { get; private set; }
        public GameVersion Version { get; private set; }
        public string InGameName { get; private set; } = "E-BOT";

        public override void SoftStop() => Config.Pause();

        public async Task<PB7> LGReadPokemon(uint offset, CancellationToken token, int size = EncryptedSize, bool heap = true)
        {
            byte[] data;
            if (heap == true)
                data = await Connection.ReadBytesAsync(offset, size, token).ConfigureAwait(false);
            else
                data = await SwitchConnection.ReadBytesMainAsync(offset, size, token).ConfigureAwait(false);
            return new PB7(data);
        }

        public async Task<PB7> LGReadPokemon(ulong offset, CancellationToken token, int size = EncryptedSize)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
            return new PB7(data);
        }

        public async Task CleanExit(CancellationToken token)
        {
            await SetScreen(ScreenState.On, token).ConfigureAwait(false);
            Log("Detaching controllers on routine exit.");
            await DetachController(token).ConfigureAwait(false);
        }

        public async Task<SAV7b> LGIdentifyTrainer(CancellationToken token)
        {
            Log("Grabbing trainer data of host console...");
            SAV7b sav = await LGGetFakeTrainerSAV(token).ConfigureAwait(false);
            GameLang = (LanguageID)sav.Language;
            Version = sav.Version;
            InGameName = sav.OT;
            Connection.Label = $"{InGameName}-{sav.DisplayTID:000000}";
            Log($"{Connection.Name} identified as {Connection.Label}, using {GameLang}.");

            return sav;
        }

        public async Task<SAV7b> LGGetFakeTrainerSAV(CancellationToken token)
        {
            SAV7b lgpe = new SAV7b();

            byte[] dest = lgpe.Blocks.Status.Data;
            int startofs = lgpe.Blocks.Status.Offset;
            byte[]? data = await Connection.ReadBytesAsync(TrainerData, TrainerSize, token).ConfigureAwait(false);
            data.CopyTo(dest, startofs);
            return lgpe;
        }

        public async Task<bool> LGIsInTitleScreen(CancellationToken token) => !((await SwitchConnection.ReadBytesMainAsync(IsInTitleScreen, 1, token).ConfigureAwait(false))[0] == 1);
        public async Task<bool> LGIsinwaitingScreen(CancellationToken token) => BitConverter.ToUInt32(await SwitchConnection.ReadBytesMainAsync(waitingscreen, 4, token).ConfigureAwait(false), 0) == 0;
        public async Task<bool> LGIsInTrade(CancellationToken token) => (await SwitchConnection.ReadBytesMainAsync(IsInTrade, 1, token).ConfigureAwait(false))[0] != 0;
        public async Task<bool> LGIsGiftFound(CancellationToken token) => (await SwitchConnection.ReadBytesMainAsync(IsGiftFound, 1, token).ConfigureAwait(false))[0] > 0;
        public async Task<uint> LGEncounteredWild(CancellationToken token) => BitConverter.ToUInt16(await Connection.ReadBytesAsync(CatchingSpecies, 2, token).ConfigureAwait(false), 0);
        public async Task<GameVersion> LGWhichGameVersion(CancellationToken token)
        {
            byte[] data = await Connection.ReadBytesAsync(LGGameVersion, 1, token).ConfigureAwait(false);
            if (data[0] == 0x01)
                return GameVersion.GP;
            else if (data[0] == 0x02)
                return GameVersion.GE;
            else
                return GameVersion.Invalid;
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

            public async Task<bool> LGIsNatureTellerEnabled(CancellationToken token) => (await Connection.ReadBytesAsync(NatureTellerEnabled, 1, token).ConfigureAwait(false))[0] == 0x04;
        public async Task<Nature> LGReadWildNature(CancellationToken token) => (Nature)BitConverter.ToUInt16(await Connection.ReadBytesAsync(WildNature, 2, token).ConfigureAwait(false), 0);
        public async Task LGEnableNatureTeller(CancellationToken token) => await Connection.WriteBytesAsync(BitConverter.GetBytes(0x04), NatureTellerEnabled, token).ConfigureAwait(false);
        public async Task LGEditWildNature(Nature target, CancellationToken token) => await Connection.WriteBytesAsync(BitConverter.GetBytes((uint)target), WildNature, token).ConfigureAwait(false);
        public async Task<uint> LGReadSpeciesCombo(CancellationToken token) =>
            BitConverter.ToUInt16(await SwitchConnection.ReadBytesAbsoluteAsync(await ParsePointer(SpeciesComboPointer, token).ConfigureAwait(false), 2, token).ConfigureAwait(false), 0);
        public async Task<uint> LGReadComboCount(CancellationToken token) =>
            BitConverter.ToUInt16(await SwitchConnection.ReadBytesAbsoluteAsync(await ParsePointer(CatchComboPointer, token).ConfigureAwait(false), 2, token).ConfigureAwait(false), 0);
        public async Task LGEditSpeciesCombo(uint species, CancellationToken token) =>
            await SwitchConnection.WriteBytesAbsoluteAsync(BitConverter.GetBytes(species), await ParsePointer(SpeciesComboPointer, token).ConfigureAwait(false), token).ConfigureAwait(false);
        public async Task LGEditComboCount(uint count, CancellationToken token) =>
            await SwitchConnection.WriteBytesAbsoluteAsync(BitConverter.GetBytes(count), await ParsePointer(CatchComboPointer, token).ConfigureAwait(false), token).ConfigureAwait(false);

        //Pointer parser, code from ALM
        public async Task<ulong> ParsePointer(String pointer, CancellationToken token)
        {
            var ptr = pointer;
            uint finadd = 0;
            if (!ptr.EndsWith("]"))
                finadd = Util.GetHexValue(ptr.Split('+').Last());
            var jumps = ptr.Replace("main", "").Replace("[", "").Replace("]", "").Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
            if (jumps.Length == 0)
            {
                Log("Invalid Pointer");
                return 0;
            }

            var initaddress = Util.GetHexValue(jumps[0].Trim());
            ulong address = BitConverter.ToUInt64(await SwitchConnection.ReadBytesMainAsync(initaddress, 0x8, token).ConfigureAwait(false), 0);
            foreach (var j in jumps)
            {
                var val = Util.GetHexValue(j.Trim());
                if (val == initaddress)
                    continue;
                if (val == finadd)
                {
                    address += val;
                    break;
                }
                address = BitConverter.ToUInt64(await SwitchConnection.ReadBytesAbsoluteAsync(address + val, 0x8, token).ConfigureAwait(false), 0);
            }
            return address;
        }
    }
}

