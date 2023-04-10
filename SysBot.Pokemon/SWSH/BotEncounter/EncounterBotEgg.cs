using PKHeX.Core;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class EncounterBotEgg : EncounterBot
    {
        private readonly IDumper DumpSetting;

        public EncounterBotEgg(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg, hub)
        {
            DumpSetting = Hub.Config.Folder;
        }

        private static readonly PK8 Blank = new();

        protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
        {
            await SetupBoxState(DumpSetting, token).ConfigureAwait(false);

            while (!token.IsCancellationRequested)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.
                var attempts = await StepUntilEgg(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    return;

                Log($"Egg available after {attempts} attempts! Clearing destination slot.");
                await SetBoxPokemon(Blank, 0, 0, token).ConfigureAwait(false);

                for (int i = 0; i < 10; i++)
                    await Click(A, 0_200, token).ConfigureAwait(false);

                // Safe to mash B from here until we get out of all menus.
                while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                    await Click(B, 0_200, token).ConfigureAwait(false);

                Log("Egg received. Checking details.");
                var pk = await ReadBoxPokemon(0, 0, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Log("No egg found in Box 1, slot 1. Ensure that the party is full. Restarting loop.");
                    continue;
                }

                if (await HandleEncounter(pk, token).ConfigureAwait(false))
                    return;
            }
        }

        private async Task<int> StepUntilEgg(CancellationToken token)
        {
            Log("Walking around until an egg is ready...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                await SetEggStepCounter(token).ConfigureAwait(false);

                // Walk diagonally left.
                await SetStick(LEFT, -19000, 19000, 0_500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                // Walk diagonally right, slightly longer to ensure we stay at the Daycare lady.
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
