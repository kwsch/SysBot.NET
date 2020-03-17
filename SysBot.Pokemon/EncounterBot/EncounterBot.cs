using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class EncounterBot : PokeRoutineExecutor
    {
        private readonly BotCompleteCounts Counts;
        private readonly IDumper DumpSetting;
        private readonly Species StopOnSpecies;
        private readonly EncounterMode Mode;
        private readonly Nature DesiredNature;
        private readonly int[] DesiredIVs = {-1, -1, -1, -1, -1, -1};

        public EncounterBot(PokeBotConfig cfg, EncounterSettings encounter, IDumper dump, BotCompleteCounts count) : base(cfg)
        {
            Counts = count;
            DumpSetting = dump;
            StopOnSpecies = encounter.StopOnSpecies;
            Mode = encounter.EncounteringType;
            DesiredNature = encounter.DesiredNature;

            /* Populate DesiredIVs array.  Bot matches 0 and 31 IVs.
             * Any other nonzero IV is treated as a minimum accepted value.
             * If they put "x", this is a wild card so we can leave -1. */
            string[] splitIVs = encounter.DesiredIVs.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            // Only accept up to 6 values in case people can't count.
            for (int i = 0; i < 6 && i < splitIVs.Length; i++)
            {
                if (splitIVs[i] == "x" || splitIVs[i] == "X")
                    continue;
                DesiredIVs[i] = Convert.ToInt32(splitIVs[i]);
            }
        }

        private int encounterCount;

        private bool StopCondition(PK8 pk)
        {
            /*  Match species and look for a shiny. This is only checked for walking encounters.
             *  If they specified a species, it has to match. */
            if (Mode == EncounterMode.HorizontalLine || Mode == EncounterMode.VerticalLine)
            {
                if (!pk.IsShiny || (StopOnSpecies != Species.None && StopOnSpecies != (Species)pk.Species))
                    return false;
            }

            if (DesiredNature != Nature.Random && DesiredNature != (Nature)pk.Nature)
                return false;

            // Reorder the Pokemon's IVs to HP/Atk/Def/SpA/SpD/Spe
            int[] pkIVList = PKX.ReorderSpeedLast(pk.IVs);

            for (int i = 0; i < 6; i++)
            {
                // Match all 0's.
                if (DesiredIVs[i] == 0 && pk.IVs[i] != 0)
                    return false;
                // Wild cards should be -1, so they will always be less than the Pokemon's IVs.
                if (DesiredIVs[i] > pkIVList[i])
                    return false;
            }
            return true;
        }

        protected override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            Log("Starting main EncounterBot loop.");

            // Clear out any residual stick weirdness.
            await ResetStick(token).ConfigureAwait(false);

            var task = Mode switch
            {
                EncounterMode.VerticalLine => WalkInLine(token),
                EncounterMode.HorizontalLine => WalkInLine(token),
                EncounterMode.Eternatus => DoEternatusEncounter(token),
                EncounterMode.LegendaryDogs => DoDogEncounter(token),
                _ => WalkInLine(token),
            };
            await task.ConfigureAwait(false);

            await ResetStick(token).ConfigureAwait(false);
        }

        private async Task WalkInLine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var attempts = await StepUntilEncounter(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    continue;

                Log($"Encounter found after {attempts} attempts! Checking details.");

                var pk = await ReadPokemon(WildPokemonOffset, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Log("Invalid data detected. Restarting loop.");
                    // add stuff for recovering
                    continue;
                }

                encounterCount++;
                Log($"Encounter: {encounterCount}{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}");
                Counts.AddCompletedEncounters();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "encounters", pk);

                if (StopCondition(pk))
                {
                    Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                    return;
                }
                else if (pk.Ability == 119 || pk.Ability == 107) // pokemon with announced abilites
                {
                    await Task.Delay(2700, token).ConfigureAwait(false);
                }

                await Task.Delay(4600, token).ConfigureAwait(false);
                while (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                    await FleeToOverworld(token).ConfigureAwait(false);

                if (Mode == EncounterMode.VerticalLine) await SetStick(LEFT, 0, -30000, 2500, token).ConfigureAwait(false);
                else if (Mode == EncounterMode.HorizontalLine) await SetStick(LEFT, -30000, 0, 2500, token).ConfigureAwait(false);
                await ResetStick(token).ConfigureAwait(false);

            }
        }

        private async Task DoEternatusEncounter(CancellationToken token)
        {
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EncounterBot)
            {
                await SetStick(LEFT, 0, 20000, 500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 1000, token).ConfigureAwait(false);

                var pk = await ReadPokemon(RaidPokemonOffset, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Connection.Log("Invalid data detected. Restarting loop.");
                    // add stuff for recovering
                    continue;
                }

                encounterCount++;
                Connection.Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                Counts.AddCompletedLegends();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "legends", pk);

                if (StopCondition(pk))
                {
                    Connection.Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                    return;
                }

                Connection.Log("Resetting raid by restarting the game");
                // Close out of the game
                await Click(HOME, 1600, token).ConfigureAwait(false);
                await Click(X, 800, token).ConfigureAwait(false);
                await Click(A, 4000, token).ConfigureAwait(false); // Closing software prompt
                Connection.Log("Closed out of the game!");

                // Open game and select profile
                await Click(A, 1000, token).ConfigureAwait(false);
                await Click(A, 1000, token).ConfigureAwait(false);
                Connection.Log("Restarting the game!");

                // Switch Logo lag, skip cutscene, game load screen
                await Task.Delay(14000, token).ConfigureAwait(false);
                await Click(A, 1000, token).ConfigureAwait(false);
                await Task.Delay(3500, token).ConfigureAwait(false);
                Connection.Log("Back in the overworld!");
                await ResetStick(token).ConfigureAwait(false);
            }
        }

        private async Task DoDogEncounter(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Log("Looking for a new dog...");

                // At the start of each loop, an A press is needed to exit out of a prompt.
                await Click(A, 0_500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 30000, 1000, token).ConfigureAwait(false);

                // Encounters Zacian/Zamazenta
                await Click(A, 0_600, token).ConfigureAwait(false);

                // Cutscene loads
                await Click(A, 3_500, token).ConfigureAwait(false);

                // Click through all the menus.
                for (int i = 0; i < 4; i++)
                    await Click(A, 1_000, token).ConfigureAwait(false);

                if (await IsCorrectDogScreen(false, token).ConfigureAwait(false))
                {
                    Log("Encounter started! Checking details.");
                }
                else
                {
                    Log("Something went wrong. Reposition and restart the bot.");
                    return;
                }

                await Task.Delay(4_000, token).ConfigureAwait(false);

                var pk = await ReadPokemon(LegendaryPokemonOffset, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Log("Invalid data detected. Restarting loop.");
                    continue;
                }

                encounterCount++;
                Log($"Encounter: {encounterCount}{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}");
                Counts.AddCompletedLegends();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "legends", pk);

                if (StopCondition(pk))
                {
                    Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                    return;
                }

                // Wait for the entire cutscene until we can flee.
                await Task.Delay(21_000, token).ConfigureAwait(false);

                // Get rid of any stick stuff left over so we can flee properly.
                await ResetStick(token).ConfigureAwait(false);

                while (!await IsCorrectDogScreen(true, token).ConfigureAwait(false))
                    await FleeToOverworld(token).ConfigureAwait(false);
            }
        }

        private async Task<bool> IsCorrectDogScreen(bool flee, CancellationToken token)
        {
            var screen = await Connection.ReadBytesAsync(CurrentScreenOffset, 4, token).ConfigureAwait(false);
            uint currentscreen = BitConverter.ToUInt32(screen, 0);
            if (!flee && ScreenIsStart(currentscreen))
                return true;
            if (flee && ScreenIsFlee(currentscreen))
                return true;
            return false;
        }

        private static bool ScreenIsStart(uint currentscreen)
        {
            return    currentscreen == CurrentScreen_Dog_Daytime_StartBattle
                   || currentscreen == CurrentScreen_Dog_Sunset_StartBattle
                   || currentscreen == CurrentScreen_Dog_Night_StartBattle
                   || currentscreen == CurrentScreen_Dog_Dawn_StartBattle;
        }

        private static bool ScreenIsFlee(uint currentscreen)
        {
            return    currentscreen == CurrentScreen_Dog_Daytime_FleeBattle_1
                   || currentscreen == CurrentScreen_Dog_Daytime_FleeBattle_2
                   || currentscreen == CurrentScreen_Dog_Sunset_FleeBattle_1
                   || currentscreen == CurrentScreen_Dog_Sunset_FleeBattle_2
                   || currentscreen == CurrentScreen_Dog_Dawn_FleeBattle_1
                   || currentscreen == CurrentScreen_Dog_Dawn_FleeBattle_2
                   || currentscreen == CurrentScreen_Dog_Night_FleeBattle_1
                   || currentscreen == CurrentScreen_Dog_Night_FleeBattle_2;
        }

        private async Task<int> StepUntilEncounter(CancellationToken token)
        {
            Log("Walking around until an encounter...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EncounterBot)
            {
                if (await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false) || await IsCorrectScreen(CurrentScreen_WildArea, token).ConfigureAwait(false))
                {
                    switch (Mode)
                    {
                        case EncounterMode.VerticalLine:
                            await SetStick(LEFT, 0, 30000, 2500, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 750, token).ConfigureAwait(false); // reset

                            await SetStick(LEFT, 0, -30000, 2500, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset
                            break;
                        case EncounterMode.HorizontalLine:
                            await SetStick(LEFT, 30000, 0, 2500, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 750, token).ConfigureAwait(false); // reset

                            await SetStick(LEFT, -30000, 0, 2500, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset
                            break;
                    }
                }
                else
                {
                    while (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                        await FleeToOverworld(token).ConfigureAwait(false);
                }

                var pk = await ReadPokemon(WildPokemonOffset, token).ConfigureAwait(false);
                if (pk.Species == 0)
                    continue;
                if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                    return attempts;

                attempts++;
                if (attempts % 10 == 0)
                    Log($"Tried {attempts} times, still no encounters.");
            }

            return -1; // aborted
        }

        private async Task ResetStick(CancellationToken token)
        {
            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 1_000, token).ConfigureAwait(false); // reset
        }

        private async Task FleeToOverworld(CancellationToken token)
        {
            // This routine will always escape a battle.
            await Click(B, 0_400, token).ConfigureAwait(false);
            await Click(B, 0_900, token).ConfigureAwait(false);
            await Click(DUP, 0_500, token).ConfigureAwait(false);
            await Click(A, 0_750, token).ConfigureAwait(false);
            await Task.Delay(2_000, token).ConfigureAwait(false);
        }
    }
}
