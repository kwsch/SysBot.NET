using PKHeX.Core;
using SysBot.Base;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor : SwitchRoutineExecutor<PokeBotConfig>
    {
        protected PokeRoutineExecutor(PokeBotConfig cfg) : base(cfg) { }

        public LanguageID GameLang;
        private GameVersion Version;
        public string InGameName = "SysBot.NET";

        public override void SoftStop() => Config.Pause();

        public async Task Click(SwitchButton b, int delayMin, int delayMax, CancellationToken token) =>
            await Click(b, Util.Rand.Next(delayMin, delayMax), token).ConfigureAwait(false);

        public async Task SetStick(SwitchStick stick, short x, short y, int delayMin, int delayMax, CancellationToken token) =>
            await SetStick(stick, x, y, Util.Rand.Next(delayMin, delayMax), token).ConfigureAwait(false);

        private static uint GetBoxSlotOffset(int box, int slot) => BoxStartOffset + (uint)(BoxFormatSlotSize * ((30 * box) + slot));

        public async Task<PK8> ReadPokemon(uint offset, CancellationToken token, int size = BoxFormatSlotSize)
        {
            var data = await Connection.ReadBytesAsync(offset, size, token).ConfigureAwait(false);
            return new PK8(data);
        }

        public async Task<PK8> ReadSurpriseTradePokemon(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(SurpriseTradePartnerPokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
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
            pkm.ResetPartyStats();
            await Connection.WriteBytesAsync(pkm.EncryptedPartyData, ofs, token).ConfigureAwait(false);
        }

        public async Task<PK8> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            var ofs = GetBoxSlotOffset(box, slot);
            return await ReadPokemon(ofs, token, BoxFormatSlotSize).ConfigureAwait(false);
        }

        public async Task SetCurrentBox(int box, CancellationToken token)
        {
            await Connection.WriteBytesAsync(BitConverter.GetBytes(box), CurrentBoxOffset, token).ConfigureAwait(false);
        }

        public async Task<int> GetCurrentBox(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(CurrentBoxOffset, 1, token).ConfigureAwait(false);
            return data[0];
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

        // Reads an offset until it changes to either match or differ from the comparison value.
        // If "match" is set to true, then the function returns true when the offset matches the given value.
        // Otherwise, it returns true when the offset no longer matches the given value.
        public async Task<bool> ReadUntilChanged(uint offset, byte[] comparison, int waitms, int waitInterval, bool match, CancellationToken token)
        {
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                var result = await Connection.ReadBytesAsync(offset, comparison.Length, token).ConfigureAwait(false);
                if (match == result.SequenceEqual(comparison))
                    return true;

                await Task.Delay(waitInterval, token).ConfigureAwait(false);
            } while (sw.ElapsedMilliseconds < waitms);
            return false;
        }

        public async Task<bool> ReadIsChanged(uint offset, byte[] original, CancellationToken token)
        {
            var result = await Connection.ReadBytesAsync(offset, original.Length, token).ConfigureAwait(false);
            return !result.SequenceEqual(original);
        }

        public async Task<bool> LinkTradePartnerFound(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LinkTradeSearchingOffset, 1, token).ConfigureAwait(false);
            return data[0] == 0;
        }

        public async Task<SAV8SWSH> IdentifyTrainer(CancellationToken token)
        {
            Log("Grabbing trainer data of host console...");
            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
            GameLang = (LanguageID)sav.Language;
            Version = sav.Version;
            InGameName = sav.OT;
            Connection.Name = $"{InGameName}-{sav.DisplayTID:000000}";
            Log($"{Connection.IP} identified as {Connection.Name}, using {GameLang}.");

            if (await GetTextSpeed(token).ConfigureAwait(false) != TextSpeed.Fast)
                Log("Text speed should be set to FAST. Stop the bot and fix this if you encounter problems.");

            return sav;
        }

        public static void DumpPokemon(string folder, string subfolder, PKM pk)
        {
            if (!Directory.Exists(folder))
                return;
            var dir = Path.Combine(folder, subfolder);
            Directory.CreateDirectory(dir);
            var fn = Path.Combine(dir, Util.CleanFileName(pk.FileName));
            File.WriteAllBytes(fn, pk.DecryptedPartyData);
        }

        /// <summary>
        /// Identifies the trainer information and loads the current runtime language.
        /// </summary>
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
                const int delay = 0_500;
                await Click(key, delay, token).ConfigureAwait(false);
            }
            // Confirm Code outside of this method (allow synchronization)
        }

        public async Task EnsureConnectedToYComm(PokeTradeHubConfig config, CancellationToken token)
        {
            if (!await IsGameConnectedToYComm(token).ConfigureAwait(false))
            {
                Log("Reconnecting to Y-Comm...");
                await ReconnectToYComm(config, token).ConfigureAwait(false);
            }
        }

        public async Task<bool> CheckTradePartnerName(TradeMethod tradeMethod, string Name, CancellationToken token)
        {
            var name = await GetTradePartnerName(tradeMethod, token).ConfigureAwait(false);
            return name == Name;
        }

        public async Task<string> GetTradePartnerName(TradeMethod tradeMethod, CancellationToken token)
        {
            var ofs = GetTrainerNameOffset(tradeMethod);
            var data = await Connection.ReadBytesAsync(ofs, 26, token).ConfigureAwait(false);
            return StringConverter.GetString7(data, 0, 26);
        }

        public async Task<bool> IsGameConnectedToYComm(CancellationToken token)
        {
            // Reads the Y-Comm Flag is the Game is connected Online
            var data = await Connection.ReadBytesAsync(IsConnectedOffset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task ReconnectToYComm(PokeTradeHubConfig config, CancellationToken token)
        {
            // Press B in case a Error Message is Present
            await Click(B, 2000, token).ConfigureAwait(false);

            // Return to Overworld
            if (!await IsOnOverworld(config, token).ConfigureAwait(false))
            {
                for (int i = 0; i < 5; i++)
                {
                    await Click(B, 500, token).ConfigureAwait(false);
                }
            }

            await Click(Y, 1000, token).ConfigureAwait(false);
            await Click(PLUS, 7_000 + config.ExtraTimeReconnectYComm, token).ConfigureAwait(false);

            for (int i = 0; i < 5; i++)
            {
                await Click(B, 500, token).ConfigureAwait(false);
            }
        }

        public async Task ExitTrade(PokeTradeHubConfig config, bool unexpected, CancellationToken token)
        {
            if (unexpected)
                Log("Unexpected behavior, recover position");

            int attempts = 0;
            uint screenID;
            int softBanAttempts = 0;
            while (!await IsOnOverworld(config, token).ConfigureAwait(false))
            {
                screenID = await GetCurrentScreen(token).ConfigureAwait(false);
                if (screenID == CurrentScreen_Softban)
                {
                    softBanAttempts++;
                    if (softBanAttempts > 10)
                        await ReOpenGame(token).ConfigureAwait(false);
                }

                attempts++;
                if (attempts >= 15)
                    break;

                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }
        }

        public async Task ExitSeedCheckTrade(PokeTradeHubConfig config, CancellationToken token)
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

        public async Task ReOpenGame(CancellationToken token)
        {
            // Reopen the game if we get softbanned
            Log("Potential softban detected, reopening game just in case!");
            await Click(HOME, 2000, token).ConfigureAwait(false);
            await Click(X, 1000, token).ConfigureAwait(false);
            await Click(A, 5000, token).ConfigureAwait(false);

            for (int i = 0; i < 30; i++)
                await Click(A, 1000, token).ConfigureAwait(false);

            // In case we are softbanned, reset the timestamp
            await Unban(token).ConfigureAwait(false);
        }

        public async Task Unban(CancellationToken token)
        {
            // Like previous Generations the Game uses a Unix Timestamp for 
            // how long we are Soft-Banned and once the Soft-Ban is lifted
            // the Game sets the value back to 0 (1970/01/01 12:00 AM (UTC) )
            var data = BitConverter.GetBytes(0);
            await Connection.WriteBytesAsync(data, SoftBanUnixTimespanOffset, token).ConfigureAwait(false);
        }

        public async Task<bool> CheckIfSoftBanned(CancellationToken token)
        {
            // Check if the Unix Timestamp isn't Zero, if so we are Softbanned.
            var data = await Connection.ReadBytesAsync(SoftBanUnixTimespanOffset, 1, token).ConfigureAwait(false);
            return data[0] > 1;
        }

        public async Task<bool> CheckIfSearchingForLinkTradePartner(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LinkTradeSearchingOffset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task<bool> CheckIfSearchingForSurprisePartner(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(SurpriseTradeSearchOffset, 8, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0) == SurpriseTradeSearch_Searching;
        }

        public async Task ResetTradePosition(PokeTradeHubConfig config, CancellationToken token)
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

        public async Task<bool> IsEggReady(SwordShieldDaycare daycare, CancellationToken token)
        {
            var ofs = GetDaycareEggIsReadyOffset(daycare);
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
            var ofs = GetDaycareStepCounterOffset(daycare);
            await Connection.WriteBytesAsync(data, ofs, token).ConfigureAwait(false);
        }

        public async Task<bool> IsCorrectScreen(uint expectedScreen, CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(CurrentScreenOffset, 4, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0) == expectedScreen;
        }

        public async Task<uint> GetCurrentScreen(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(CurrentScreenOffset, 4, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0);
        }

        public async Task<bool> IsInBattle(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(InBattleRaidOffset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task<bool> IsInBox(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(CurrentScreenOffset, 4, token).ConfigureAwait(false);
            var dataint = BitConverter.ToUInt32(data, 0);
            return dataint == CurrentScreen_Box1 || dataint == CurrentScreen_Box2;
        }

        public async Task<bool> IsOnOverworld(PokeTradeHubConfig config, CancellationToken token)
        {
            // Uses CurrentScreenOffset and compares the value to CurrentScreen_Overworld.
            if (config.ScreenDetection == ScreenDetectionMode.Original)
            {
                var data = await Connection.ReadBytesAsync(CurrentScreenOffset, 4, token).ConfigureAwait(false);
                var dataint = BitConverter.ToUInt32(data, 0);
                return dataint == CurrentScreen_Overworld1 || dataint == CurrentScreen_Overworld2;
            }
            // Uses an appropriate OverworldOffset for the console language.
            else if (config.ScreenDetection == ScreenDetectionMode.ConsoleLanguageSpecific)
            {
                var data = await Connection.ReadBytesAsync(GetOverworldOffset(config.ConsoleLanguage), 1, token).ConfigureAwait(false);
                return data[0] == 1;
            }
            return false;
        }

        public async Task<TextSpeed> GetTextSpeed(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(TextSpeedOffset, 1, token).ConfigureAwait(false);
            return (TextSpeed)(data[0] & 3);
        }

        public async Task SetTextSpeed(TextSpeed speed, CancellationToken token)
        {
            var textSpeedByte = await Connection.ReadBytesAsync(TextSpeedOffset, 1, token).ConfigureAwait(false);
            var data = new[] { (byte)((textSpeedByte[0] & 0xFC) | (int)speed) };
            await Connection.WriteBytesAsync(data, TextSpeedOffset, token).ConfigureAwait(false);
        }

        public static uint GetOverworldOffset(ConsoleLanguageParameter value)
        {
            return value switch
            {
                ConsoleLanguageParameter.French => OverworldOffsetFrench,
                ConsoleLanguageParameter.German => OverworldOffsetGerman,
                ConsoleLanguageParameter.Spanish => OverworldOffsetSpanish,
                ConsoleLanguageParameter.Italian => OverworldOffsetItalian,
                ConsoleLanguageParameter.Japanese => OverworldOffsetJapanese,
                ConsoleLanguageParameter.ChineseTraditional => OverworldOffsetChineseT,
                ConsoleLanguageParameter.ChineseSimplified => OverworldOffsetChineseS,
                ConsoleLanguageParameter.Korean => OverworldOffsetKorean,
                _ => OverworldOffset,
            };
        }

        public async Task<SlotQualityCheck> GetBoxSlotQuality(int box, int slot, CancellationToken token)
        {
            var result = await ReadBoxPokemon(box, slot, token).ConfigureAwait(false);
            return new SlotQualityCheck(result);
        }

        public void PrintBadSlotMessage(SlotQualityCheck q)
        {
            switch (q.Quality)
            {
                case SlotQuality.BadData:
                    Log("Garbage detected in required Box Slot. Preventing execution.");
                    return;
                case SlotQuality.HasData:
                    Log("Required Box Slot not empty. Move this Pokémon before using the bot!");
                    Log(new ShowdownSet(q.Data!).Text);
                    return;
            }
        }
    }
}
