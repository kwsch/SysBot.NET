using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor : SwitchRoutineExecutor<PokeBotConfig>
    {
        protected PokeRoutineExecutor(PokeBotConfig cfg) : base(cfg) { }

        public async Task Click(SwitchButton b, int delayMin, int delayMax, CancellationToken token) =>
            await Click(b, Util.Rand.Next(delayMin, delayMax), token).ConfigureAwait(false);

        public async Task SetStick(SwitchStick stick, int x, int y, int delayMin, int delayMax, CancellationToken token) =>
            await SetStick(stick, x, y, Util.Rand.Next(delayMin, delayMax), token).ConfigureAwait(false);

        private static uint GetBoxSlotOffset(int box, int slot) => Box1Slot1 + (uint)(BoxFormatSlotSize * ((30 * box) + slot));

        public async Task<PK8> ReadPokemon(uint offset, CancellationToken token, int size = BoxFormatSlotSize)
        {
            var data = await Connection.ReadBytesAsync(offset, size, token).ConfigureAwait(false);
            return new PK8(data);
        }

        public async Task SetBoxPokemon(PK8 pkm, int box, int slot, CancellationToken token, SAV8? sav = null)
        {
            if (sav != null)
            {
                // Update PKM to the current save's handler data
                DateTime Date = DateTime.Now;
                pkm.Trade(sav, Date.Day, Date.Month, Date.Year);
                pkm.RefreshChecksum();
            }
            var ofs = GetBoxSlotOffset(box, slot);
            await Connection.WriteBytesAsync(pkm.EncryptedPartyData, ofs, token).ConfigureAwait(false);
        }

        public async Task<PK8> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            var ofs = GetBoxSlotOffset(box, slot);
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

        public async Task<SAV8SWSH> IdentifyTrainer(CancellationToken token)
        {
            Connection.Log("Grabbing trainer data of host console...");
            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
            Connection.Name = $"{sav.OT}-{sav.DisplayTID}";
            Connection.Log($"{Connection.IP} identified as {Connection.Name}");
            return sav;
        }

        public static void DumpPokemon(string? folder, PKM pk)
        {
            if (folder == null || !Directory.Exists(folder))
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
            var keys = TradeUtil.GetPresses(code);
            foreach (var key in keys)
            {
                var delay = key == A ? 1_500 : 0_500;
                await Click(key, delay, token).ConfigureAwait(false);
            }
            // Confirm Code outside of this method (allow synchronization)
        }

        public async Task EnsureConnectedToYCom(CancellationToken token)
        {
            if (!await IsGameConnectedToYCom(token).ConfigureAwait(false))
            {
                Connection.Log("Reconnecting to Y-Com...");
                await ReconnectToYCom(token).ConfigureAwait(false);
            }
        }

        public async Task<bool> CheckTradePartnerName(string Name, CancellationToken token)
        {
            var name = await GetTradePartnerName(token).ConfigureAwait(false);
            return name == Name;
        }

        public async Task<string> GetTradePartnerName(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(TradePartnerNameOffset, 26, token).ConfigureAwait(false);
            return StringConverter.GetString7(data, 0, 26);
        }

        public async Task<bool> IsGameConnectedToYCom(CancellationToken token)
        {
            // Reads the Y-Com Flag is the Game is connected Online
            var data = await Connection.ReadBytesAsync(IsConnected, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task ReconnectToYCom(CancellationToken token)
        {
            // Press B in case a Error Message is Present
            await Click(B, 1000, token).ConfigureAwait(false);

            await Click(Y, 1000, token).ConfigureAwait(false);
            await Click(PLUS, 15_000, token).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
            {
                await Click(B, 500, token).ConfigureAwait(false);
            }
        }

        public async Task ExitTrade(uint overworld, CancellationToken token)
        {
            while (!await CheckScreenState(overworld, token).ConfigureAwait(false))
            {
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }
        }

        public async Task ResetTradePosition(CancellationToken token)
        {
            await Click(Y, 1_000, token).ConfigureAwait(false);
            for (int i = 0; i < 5; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(B, 1_000, token).ConfigureAwait(false);
            await Click(B, 1_000, token).ConfigureAwait(false);
        }

        public async Task<bool> IsEggReady(SwordShieldDaycare daycare, CancellationToken token)
        {
            var ofs = GetDaycareOffset(daycare);
            // Read a single byte of the Daycare metadata to check the IsEggReady flag.
            var data = await Connection.ReadBytesAsync(ofs, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task SetEggStepCounter(SwordShieldDaycare daycare, CancellationToken token)
        {
            // Set the step counter in the Daycare metadata to 180. This is the threshold that triggers the "Should I create a new egg" subroutine.
            // When the game executes the subroutine, it will generate a new seed and set the IsEggReady flag.
            // Just setting the IsEggReady flag won't refresh the seed; we want a different egg every time.
            var data = new byte[] { 0xB4, 0, 0, 0 }; // 180
            var ofs = GetDaycareOffset(daycare);
            await Connection.WriteBytesAsync(data, ofs, token).ConfigureAwait(false);
        }

        public async Task<uint> SetupScreenDetection(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(ScreenStateOffset, 2, token).ConfigureAwait(false);
            uint StartValue = BitConverter.ToUInt16(data, 0);
            Overworld = StartValue;
            return Overworld;
        }

        public async Task<bool> CheckScreenState(uint expectedScreen, CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(ScreenStateOffset, 2, token).ConfigureAwait(false);
            return BitConverter.ToUInt16(data, 0) == expectedScreen;
        }

        public async Task GetScreenState(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(ScreenStateOffset,4, token).ConfigureAwait(false);
            uint State = BitConverter.ToUInt16(data, 0);

            var status = "No Status";
            if (State == Overworld) status = "Overworld";
            if (State == BoxView) status = "Viewing the box";
            if (State == TradeEvo) status = "In a Trade Evolution";
            if (State == DuringTrade) status = "Trading";

            Connection.Log(status);
        }

        public async Task<SlotQualityCheck> GetBoxSlotQuality(int box, int slot, CancellationToken token)
        {
            var result = await ReadBoxPokemon(box, slot, token).ConfigureAwait(false);
            return new SlotQualityCheck(result);
        }

        public static int[] GetBlankIVTemplate() => new[] { -1, -1, -1, -1, -1, -1 };

        public void PrintBadSlotMessage(SlotQualityCheck q)
        {
            switch (q.Quality)
            {
                case SlotQuality.BadData:
                    Connection.Log("Garbage detected in required Box Slot. Preventing execution.");
                    return;
                case SlotQuality.HasData:
                    Connection.Log("Required Box Slot not empty. Move this Pokemon before using the bot!");
                    Connection.Log(new ShowdownSet(q.Data!).Text);
                    return;
            }
        }
    }
}
