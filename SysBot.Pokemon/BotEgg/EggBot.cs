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
        private readonly BotCompleteCounts Counts;
        public readonly IDumper DumpSetting;
        private const SwordShieldDaycare Location = SwordShieldDaycare.Route5;

        public EggBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Counts = hub.Counts;
            DumpSetting = hub.Config;
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public Func<PK8, bool> StopCondition { private get; set; } = pkm => pkm.IsShiny;

        protected override async Task MainLoop(CancellationToken token)
        {
            Connection.Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            Connection.Log("Checking destination slot for eggs to see if anything is in the slot...");
            var existing = await GetBoxSlotQuality(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Quality != SlotQuality.Overwritable)
            {
                PrintBadSlotMessage(existing);
                return;
            }

            Connection.Log("Starting main EggBot loop.");
            var blank = new PK8();
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.

                var attempts = await StepUntilEgg(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    continue;

                Connection.Log($"Egg available after {attempts} attempts! Clearing destination slot.");
                await SetBoxPokemon(blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

                for (int i = 0; i < 4; i++)
                    await Click(A, 500, token).ConfigureAwait(false);
                await Task.Delay(4000, token).ConfigureAwait(false);

                await Click(A, 1850, token).ConfigureAwait(false);
                await Click(A, 1850, token).ConfigureAwait(false);
                await Click(A, 450, token).ConfigureAwait(false);

                Connection.Log("Egg received. Checking details.");
                var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Connection.Log("Invalid data detected in destination slot. Restarting loop.");
                    continue;
                }

                Connection.Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                Counts.AddCompletedEggs();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "egg", pk);

                encounterCount++;
                if (!StopCondition(pk))
                    continue;

                Connection.Log("Result found! Stopping routine execution; re-start the bot(s) to search again.");
                break;
            }

            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0, CancellationToken.None).ConfigureAwait(false); // reset
        }

        private async Task<int> StepUntilEgg(CancellationToken token)
        {
            Connection.Log("Walking around until an egg is ready...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                await SetEggStepCounter(Location, token).ConfigureAwait(false);

                // Walk Diagonally Left
                await SetStick(LEFT, -19000, 19000, 500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                // Walk Diagonally Right, slightly longer to ensure we stay at the Daycare lady.
                await SetStick(LEFT, 19000, 19000, 550, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                bool eggReady = await IsEggReady(Location, token).ConfigureAwait(false);
                if (eggReady)
                    return attempts;

                attempts++;
                if (attempts % 10 == 0)
                    Connection.Log($"Tried {attempts} times, still no egg.");
            }

            return -1; // aborted
        }
    }
}
