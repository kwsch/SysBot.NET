using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor8, IEncounterBot
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly IDumper DumpSetting;
        private readonly int[] DesiredMinIVs;
        private readonly int[] DesiredMaxIVs;
        private readonly EggSettings Settings;
        public ICountSettings Counts => Settings;

        public EggBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Settings = Hub.Config.Egg;
            DumpSetting = Hub.Config.Folder;
            StopConditionSettings.InitializeTargetIVs(Hub, out DesiredMinIVs, out DesiredMaxIVs);
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        private static readonly PK8 Blank = new();

        public override async Task MainLoop(CancellationToken token)
        {
            await InitializeHardware(Hub.Config.Egg, token).ConfigureAwait(false);

            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            await SetupBoxState(token).ConfigureAwait(false);

            Log("Starting main EggBot loop.");
            Config.IterateNextRoutine();
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                try
                {
                    if (!await InnerLoop(token).ConfigureAwait(false))
                        break;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Log(e.Message);
                }
            }

            Log($"Ending {nameof(EggBot)} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0, CancellationToken.None).ConfigureAwait(false); // reset
            await CleanExit(Hub.Config.Trade, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Return true if we need to stop looping.
        /// </summary>
        private async Task<bool> InnerLoop(CancellationToken token)
        {
            // Walk a step left, then right => check if egg was generated on this attempt.
            // Repeat until an egg is generated.

            var attempts = await StepUntilEgg(token).ConfigureAwait(false);
            if (attempts < 0) // aborted
                return true;

            Log($"Egg available after {attempts} attempts! Clearing destination slot.");
            await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

            for (int i = 0; i < 6; i++)
                await Click(A, 0_400, token).ConfigureAwait(false);

            // Safe to mash B from here until we get out of all menus.
            while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                await Click(B, 0_400, token).ConfigureAwait(false);

            Log("Egg received. Checking details.");
            var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (pk.Species == 0)
            {
                Log("Invalid data detected in destination slot. Restarting loop.");
                return true;
            }

            encounterCount++;
            var print = Hub.Config.StopConditions.GetPrintName(pk);
            Log($"Encounter: {encounterCount}{Environment.NewLine}{print}{Environment.NewLine}");
            Settings.AddCompletedEggs();

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, "egg", pk);

            if (!StopConditionSettings.EncounterFound(pk, DesiredMinIVs, DesiredMaxIVs, Hub.Config.StopConditions, null))
                return true;

            // no need to take a video clip of us receiving an egg.
            var mode = Settings.ContinueAfterMatch;
            var msg = $"Result found!\n{print}\n" + mode switch
            {
                ContinueAfterMatch.Continue             => "Continuing...",
                ContinueAfterMatch.PauseWaitAcknowledge => "Waiting for instructions to continue.",
                ContinueAfterMatch.StopExit             => "Stopping routine execution; restart the bot to search again.",
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (!string.IsNullOrWhiteSpace(Hub.Config.StopConditions.MatchFoundEchoMention))
                msg = $"{Hub.Config.StopConditions.MatchFoundEchoMention} {msg}";
            EchoUtil.Echo(msg);
            Log(msg);

            if (mode == ContinueAfterMatch.StopExit)
                return false;
            if (mode == ContinueAfterMatch.Continue)
                return true;

            IsWaiting = true;
            while (IsWaiting)
                await Task.Delay(1_000, token).ConfigureAwait(false);
            return false;
        }

        private async Task SetupBoxState(CancellationToken token)
        {
            await SetCurrentBox(0, token).ConfigureAwait(false);

            var existing = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Species != 0 && existing.ChecksumValid)
            {
                Log("Destination slot is occupied! Dumping the Pokémon found there...");
                DumpPokemon(DumpSetting.DumpFolder, "saved", existing);
            }

            Log("Clearing destination slot to start the bot.");
            await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);
        }

        private bool IsWaiting;
        public void Acknowledge() => IsWaiting = false;

        private async Task<int> StepUntilEgg(CancellationToken token)
        {
            Log("Walking around until an egg is ready...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                await SetEggStepCounter(token).ConfigureAwait(false);

                // Walk Diagonally Left
                await SetStick(LEFT, -19000, 19000, 0_500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                // Walk Diagonally Right, slightly longer to ensure we stay at the Daycare lady.
                await SetStick(LEFT, 19000, 19000, 0_550, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                bool eggReady = await IsEggReady(token).ConfigureAwait(false);
                if (eggReady)
                    return attempts;

                attempts++;
                if (attempts % 10 == 0)
                    Log($"Tried {attempts} times, still no egg.");

                if (attempts > 10)
                    await Click(B, 500, token).ConfigureAwait(false);
            }

            return -1; // aborted
        }

        public async Task<bool> IsEggReady(CancellationToken token)
        {
            // Read a single byte of the Daycare metadata to check the IsEggReady flag.
            var data = await Connection.ReadBytesAsync(DayCare_Route5_Egg_Is_Ready, 1, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        public async Task SetEggStepCounter(CancellationToken token)
        {
            // Set the step counter in the Daycare metadata to 180. This is the threshold that triggers the "Should I create a new egg" subroutine.
            // When the game executes the subroutine, it will generate a new seed and set the IsEggReady flag.
            // Just setting the IsEggReady flag won't refresh the seed; we want a different egg every time.
            var data = new byte[] { 0xB4, 0, 0, 0 }; // 180
            await Connection.WriteBytesAsync(data, DayCare_Route5_Step_Counter, token).ConfigureAwait(false);
        }
    }
}
