using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly BotCompleteCounts Counts;
        private readonly IDumper DumpSetting;
        private readonly int[] DesiredIVs;
        private const SwordShieldDaycare Location = SwordShieldDaycare.Route5;

        public EggBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Counts = Hub.Counts;
            DumpSetting = Hub.Config.Folder;
            DesiredIVs = StopConditionSettings.InitializeTargetIVs(Hub);
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        private static readonly PK8 Blank = new();

        public override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            await SetCurrentBox(0, token).ConfigureAwait(false);

            var existing = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Species != 0 && existing.ChecksumValid)
            {
                Log("Destination slot is occupied! Dumping the Pokémon found there...");
                DumpPokemon(DumpSetting.DumpFolder, "saved", existing);
            }
            Log("Clearing destination slot to start the bot.");
            await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

            Log("Starting main EggBot loop.");
            Config.IterateNextRoutine();
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.

                var attempts = await StepUntilEgg(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    continue;

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
                    continue;
                }

                encounterCount++;
                Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownParsing.GetShowdownText(pk)}{Environment.NewLine}");
                Counts.AddCompletedEggs();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "egg", pk);

                if (StopConditionSettings.EncounterFound(pk, DesiredIVs, Hub.Config.StopConditions))
                {
                    if (Hub.Config.Egg.ContinueAfterMatch)
                    {
                        Log("Result found! Continuing to collect more eggs.");
                        continue;
                    }
                    Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                    await DetachController(token).ConfigureAwait(false);
                    return;
                }
            }

            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0, CancellationToken.None).ConfigureAwait(false); // reset
        }

        private async Task<int> StepUntilEgg(CancellationToken token)
        {
            Log("Walking around until an egg is ready...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                await SetEggStepCounter(Location, token).ConfigureAwait(false);

                // Walk Diagonally Left
                await SetStick(LEFT, -19000, 19000, 0_500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                // Walk Diagonally Right, slightly longer to ensure we stay at the Daycare lady.
                await SetStick(LEFT, 19000, 19000, 0_550, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                bool eggReady = await IsEggReady(Location, token).ConfigureAwait(false);
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
    }
}
