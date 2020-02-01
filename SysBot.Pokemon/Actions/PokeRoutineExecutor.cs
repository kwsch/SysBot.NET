using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor : SwitchRoutineExecutor
    {
        public const uint Box1Slot1 = 0x4293D8B0;
        public const uint TrainerDataOffset = 0x42935E48;
        public const uint ShownTradeDataOffset = 0xAC843F68;

        public const int BoxFormatSlotSize = 0x158;
        public const int TrainerDataLength = 0x110;

        protected PokeRoutineExecutor(string ip, int port) : base(ip, port) { }

        public async Task Click(SwitchButton b, int delayMin, int delayMax, CancellationToken token) =>
            await Click(b, Util.Rand.Next(delayMin, delayMax), token).ConfigureAwait(false);

        public async Task<PK8> ReadPokemon(uint offset, CancellationToken token, int size = BoxFormatSlotSize)
        {
            var data = await Connection.ReadBytesAsync(offset, size, token).ConfigureAwait(false);
            return new PK8(data);
        }

        public async Task<PK8> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            var ofs = Box1Slot1 + (uint)(BoxFormatSlotSize * ((30 * box) + slot));
            return await ReadPokemon(ofs, token, BoxFormatSlotSize).ConfigureAwait(false);
        }

        public async Task<PK8?> ReadUntilPresent(uint offset, int waitms, int waitInterval, CancellationToken token, int size = BoxFormatSlotSize)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var pk = await ReadPokemon(offset, token, size).ConfigureAwait(false);
                if (pk.Species != 0 && pk.ChecksumValid)
                    return pk;
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }
            return null;
        }

        public async Task<bool> ReadUntilChanged(uint offset, byte[] original, int waitms, int waitInterval, CancellationToken token)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var result = await Connection.ReadBytesAsync(offset, original.Length, token).ConfigureAwait(false);
                if (!result.SequenceEqual(original))
                    return true;
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }
            return false;
        }

        public async Task ReadDumpB1S1(string? folder, CancellationToken token)
        {
            if (folder == null)
                return;

            // get pokemon from box1slot1
            var pk8 = await ReadBoxPokemon(0, 0, token).ConfigureAwait(false);
            DumpPokemon(folder, pk8);
        }

        private static void DumpPokemon(string? folder, PKM pk)
        {
            if (folder == null)
                return;
            File.WriteAllBytes(Path.Combine(folder, Util.CleanFileName(pk.FileName)), pk.DecryptedPartyData);
        }

        public async Task<SAV8SWSH> GetFakeTrainerSAV(CancellationToken token)
        {
            var sav = new SAV8SWSH();
            var info = sav.MyStatus;
            var read = await Connection.ReadBytesAsync(TrainerDataOffset, TrainerDataLength, token).ConfigureAwait(false);
            read.CopyTo(info.Data);
            return sav;
        }

        protected async Task EnterTradeCode(int code, CancellationToken token)
        {
            for (int i = 0; i < 4; i++)
            {
                // Go to 0
                foreach (var e in arr[0])
                    await Click(e, 1000, token).ConfigureAwait(false);

                var digit = TradeUtil.GetCodeDigit(code, i);
                var entry = arr[digit];
                foreach (var e in entry)
                    await Click(e, 500, token).ConfigureAwait(false);

                // Confirm Digit
                await Click(A, 1_500, token).ConfigureAwait(false);
            }

            // Confirm Code outside of this method (allow synchronization)
        }

        private static readonly SwitchButton[][] arr =
        {
            new[] {DDOWN, DDOWN, DDOWN }, // 0
            new[] {DUP, DUP, DUP, DLEFT}, // 1
            new[] {DUP, DUP, DUP,      }, // 2
            new[] {DUP, DUP, DUP,DRIGHT}, // 3
            new[] {DUP, DUP, DLEFT,    }, // 4
            new[] {DUP, DUP,           }, // 5
            new[] {DUP, DUP, DRIGHT,   }, // 6
            new[] {DUP, DLEFT,         }, // 7
            new[] {DUP,                }, // 8
            new[] {DUP, DRIGHT         }, // 9
        };
    }
}