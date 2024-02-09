using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Pokemon.PokeDataOffsetsLGPE;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor7LGPE : PokeRoutineExecutor<PB7>
    {
        protected PokeRoutineExecutor7LGPE(PokeBotState cfg) : base(cfg)
        {
        }
        public ulong BoxStart = 0x533675B0;
        public readonly int SlotSize = 260;
        public int SlotCount = 25;
        public int GapSize = 380;
        public ulong GetBoxOffset(int box) => BoxStart + (ulong)((SlotSize + GapSize) * SlotCount * box);
        public ulong GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (ulong)((SlotSize + GapSize) * slot);
        public override async Task<PB7> ReadPokemon(ulong offset, CancellationToken token) => await ReadPokemon(offset, BoxFormatSlotSize, token).ConfigureAwait(false);

        public override async Task<PB7> ReadPokemon(ulong offset, int size, CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync((uint)offset, size, token).ConfigureAwait(false);
            return new PB7(data);
        }

        public async Task WriteBoxPokemon(PB7 pk,int box, int slot, CancellationToken token)
        {
            var slotofs = GetSlotOffset(box, slot);
            var StoredLength = SlotSize - 0x1c;
            await Connection.WriteBytesAsync(pk.EncryptedPartyData.AsSpan(0, StoredLength).ToArray(), (uint)slotofs, token);
            await Connection.WriteBytesAsync(pk.EncryptedPartyData.AsSpan(StoredLength).ToArray(), (uint)(slotofs + (ulong)StoredLength + 0x70),token);

        }
       
        public override async Task<PB7> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            throw new NotImplementedException();
        }

        public override Task<PB7> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public async Task<PB7?> ReadUntilPresent(uint offset, int waitms, int waitInterval, CancellationToken token, int size = BoxFormatSlotSize)
        {
            int msWaited = 0;
            while (msWaited < waitms)
            {
                var pk = await ReadPokemon(offset, size, token).ConfigureAwait(false);
                if (pk.Species != 0 && pk.ChecksumValid)
                    return pk;
                await Task.Delay(waitInterval, token).ConfigureAwait(false);
                msWaited += waitInterval;
            }
            return null;
        }

        public async Task<SAV7b> IdentifyTrainer(CancellationToken token)
        {
            // Check title so we can warn if mode is incorrect.
            string title = await SwitchConnection.GetTitleID(token).ConfigureAwait(false);
            if (title != LetsGoEeveeID && title != LetsGoPikachuID)
                throw new Exception($"{title} is not a valid Pokémon: Let's Go title. Is your mode correct?");

            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
            InitSaveData(sav);

            if (!IsValidTrainerData())
                throw new Exception("Trainer data is not valid. Refer to the SysBot.NET wiki for bad or no trainer data.");
            if (await GetTextSpeed(token).ConfigureAwait(false) < TextSpeedOption.Fast)
                throw new Exception("Text speed should be set to FAST. Fix this for correct operation.");

            return sav;
        }

        public async Task<SAV7b> GetFakeTrainerSAV(CancellationToken token)
        {
            var sav = new SAV7b();
            var info = sav.Blocks.Status;
            var read = await Connection.ReadBytesAsync(TrainerDataOffset, TrainerDataLength, token).ConfigureAwait(false);
            read.CopyTo(info.Data, info.Offset);
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
            await SetController(ControllerType.JoyRight1, token);
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

        public async Task CloseGame(PokeTradeHubConfig config, CancellationToken token)
        {
            var timing = config.Timings;
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

            await Click(A, 1_000, token).ConfigureAwait(false);

            Log("Restarting the game!");
            await Task.Delay(4_000 + timing.ExtraTimeLoadGame, token).ConfigureAwait(false);
            await DetachController(token).ConfigureAwait(false);

            while (!await IsOnOverworldStandard(token).ConfigureAwait(false))
                await Click(A, 1_000, token).ConfigureAwait(false);

            Log("Back in the overworld!");
        }

        public async Task MinimizeGame(CancellationToken token)
        {
            await Click(HOME, 1_600, token).ConfigureAwait(false);
            await DetachController(token).ConfigureAwait(false);
        }

        public async Task<bool> IsOnOverworldStandard(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LGPEStandardOverworldOffset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task<bool> IsOnOverworldBattle(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LGPEBattleOverworldOffset, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task<bool> IsOnFleeMenu(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(FleeMenuOffset, 1, token).ConfigureAwait(false);
            return data[0] == 0x10;
        }

        public async Task<TextSpeedOption> GetTextSpeed(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(TextSpeedOffset, 1, token).ConfigureAwait(false);
            return (TextSpeedOption)(data[0] & 3);
        }

        public int GetAdvancesPassed(ulong prevs0, ulong prevs1, ulong news0, ulong news1)
        {
            if (prevs0 == news0 && prevs1 == news1)
                return 0;

            var rng = new Xoroshiro128Plus(prevs0, prevs1);
            for (int i = 0; ; i++)
            {
                rng.Next();
                var (s0, s1) = rng.GetState();
                if (s0 == news0 && s1 == news1)
                    return i + 1;
                if (i > 500)
                {
                    Log("Could not find the next RNG state in 500 advances!");
                    return -1;
                }
            }
        }

       /// public async Task ActivateCatchCombo(PokeTradeHub<PB7> hub, bool activate, CancellationToken token)
      ///  {
       ///     var msg = activate ? "Activating" : "Deactivating";
        //    var msgspecies = $" for {hub.Config.EncounterLGPE.CatchComboSpecies}";
       //     Log($"{msg} Catch Combo{msgspecies}.");

      //      var species = activate ? (int)hub.Config.EncounterLGPE.CatchComboSpecies : 0;
      //      var data = BitConverter.GetBytes(species);
      //      await Connection.WriteBytesAsync(data, LGPECatchComboPokemon, token).ConfigureAwait(false);

       //     var combo = activate ? hub.Config.EncounterLGPE.CatchComboLength : 0;
      //      data = BitConverter.GetBytes(combo);
        //    await Connection.WriteBytesAsync(data, LGPECatchComboCounter, token).ConfigureAwait(false);
      //  }

        public async Task SetLure(bool activate, CancellationToken token)
        {
            var msg = activate ? "Activating" : "Deactivating";
            Log($"{msg} Max Lure.");

            var lure_type = activate ? 902 : 0; // Max Lure
            var data = BitConverter.GetBytes(lure_type);
            await Connection.WriteBytesAsync(data, LGPELureType, token).ConfigureAwait(false);

            var duration = activate ? 999 : 0;
            data = BitConverter.GetBytes(duration);
            await Connection.WriteBytesAsync(data, LGPELureCounter, token).ConfigureAwait(false);
        }

       // public async Task SetFortuneTeller(PokeTradeHub<PB7> hub, bool activate, CancellationToken token)
       // {
       //     var msg = activate ? "Activating" : "Deactivating";
       //     Log($"{msg} Fortune Teller.");

       //     var bytedata = activate ? (byte)0x4 : (byte)0x0;
       //     var data = new byte[] { bytedata };
        //    await Connection.WriteBytesAsync(data, FortuneTellerEnabled, token).ConfigureAwait(false);

        //    if (!activate)
       //         return;

         //   Log($"Setting Fortune Teller nature to {hub.Config.EncounterLGPE.FortuneTellerNature}.");
        //    data = BitConverter.GetBytes((int)hub.Config.EncounterLGPE.FortuneTellerNature);
         //   await Connection.WriteBytesAsync(data, FortuneTellerNature, token).ConfigureAwait(false);
       // }

      //  public async Task RefreshEncounterSettings(PokeTradeHub<PB7> hub, bool catch_combo, bool lure, bool fortune_teller, CancellationToken token)
       // {
        //    var encounter_settings = hub.Config.EncounterLGPE;

        //    if (catch_combo && encounter_settings.CatchComboSpecies != Species.None)
        //    {
                /// Initialize Catch Combo settings once. Can turn it on or off.
         //       var activate = encounter_settings.CatchComboLength > 0;
         //       await ActivateCatchCombo(hub, activate, token).ConfigureAwait(false);
        //    }

            // Initialize lure state. We can pass in different variables for whether we do this or not.
        //    await SetLure(lure, token).ConfigureAwait(false);

        //    if (fortune_teller)
        //    {
                /// Initialize Fortune Teller state. Will need to be renewed every day or so for long-running bots.
         //       var activate = encounter_settings.FortuneTellerNature != Nature.Random;
            //    await SetFortuneTeller(hub, activate, token).ConfigureAwait(false);
          //  }
      //  }

        public async Task<(int species, int form)> GetLastSpawnedSpecies(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LGPELastSpawnSpeciesOffset, 4, token).ConfigureAwait(false);
            var species = BitConverter.ToUInt16(data, 0);
            var form = BitConverter.ToUInt16(data, 2);
            return (species, form);
        }

        public async Task ClearLastSpawnedSpecies(CancellationToken token)
        {
            var data = BitConverter.GetBytes(0);
            await Connection.WriteBytesAsync(data, LGPELastSpawnSpeciesOffset, token).ConfigureAwait(false);
        }

        public async Task<uint> GetLastSpawnedFlags(CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LGPELastSpawnFlags, 2, token).ConfigureAwait(false);
            return BitConverter.ToUInt16(data, 0);
        }

        public async Task SetMainLoopSleepTime(int value, CancellationToken token)
        {
            var cmd = SwitchCommand.Configure(SwitchConfigureParameter.mainLoopSleepTime, value, UseCRLF);
            await Connection.SendAsync(cmd, token).ConfigureAwait(false);
        }
        public async Task<bool> LGIsinwaitingScreen(CancellationToken token) => BitConverter.ToUInt32(await SwitchConnection.ReadBytesMainAsync(waitingscreen, 4, token).ConfigureAwait(false), 0) == 0;

        public async Task RestartGameLGPE(PokeTradeHubConfig config, CancellationToken token)
        {
            await CloseGame(config, token);
            await StartGame(config, token);
        }
    }
}
