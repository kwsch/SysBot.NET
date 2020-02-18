using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor, IDumper
    {
        private readonly BotCompleteCounts Counts;

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        public string DumpFolder { get; set; } = string.Empty;

        /// <summary>
        /// Determines if it should dump received trade data.
        /// </summary>
        /// <remarks>If false, will skip dumping.</remarks>
        public bool Dump { get; set; } = false;

        private const SwordShieldDaycare Location = SwordShieldDaycare.Route5;

        public EggBot(PokeBotConfig cfg, BotCompleteCounts counts) : base(cfg) => Counts = counts;

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public Func<PK8, bool> StopCondition { private get; set; } = pkm => pkm.IsShiny;

        protected override async Task MainLoop(CancellationToken token)
        {
            await EchoCommands(false, token).ConfigureAwait(false);
            await IdentifyTrainer(token).ConfigureAwait(false);

            var existing = await GetBoxSlotQuality(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Quality != SlotQuality.Overwritable)
            {
                PrintBadSlotMessage(existing);
                return;
            }
            var blank = new PK8();
            while (!token.IsCancellationRequested)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.
                int attempts = 0;
                while (true)
                {
                    await SetEggStepCounter(Location, token).ConfigureAwait(false);

                    // Walk Diagonally Left
                    await SetStick(LEFT, -19000, 19000, 500, token).ConfigureAwait(false);
                    await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                    // Walk Diagonally Right, slightly longer to ensure we stay at the Daycare lady.
                    await SetStick(LEFT, 19000, 19000, 550, token).ConfigureAwait(false);
                    await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                    bool checkEgg = await IsEggReady(Location, token).ConfigureAwait(false);
                    if (checkEgg)
                        break;

                    attempts++;
                    if (attempts % 10 == 0)
                        Connection.Log($"Tried {attempts} times, still no egg.");
                }

                Connection.Log("Egg available! Clearing destination slot.");
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

                if (Dump && !string.IsNullOrEmpty(DumpFolder))
                    DumpPokemon(DumpFolder, pk);

                encounterCount++;
                if (!StopCondition(pk))
                    continue;

                Connection.Log("Result found! Stopping routine execution; re-start the bot(s) to search again.");
                break;
            }

            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0, CancellationToken.None).ConfigureAwait(false); // reset
        }
    }
}
