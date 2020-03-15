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
        private readonly EncounterSettings Settings;
        private readonly Species StopOnSpecies;
        private readonly EncounterMode Mode;

        public EncounterBot(PokeBotConfig cfg, EncounterSettings encounter, IDumper dump, BotCompleteCounts count) : base(cfg)
        {
            Counts = count;
            DumpSetting = dump;
            Settings = encounter; //not used right now
            StopOnSpecies = encounter.StopOnSpecies;
            Mode = encounter.EncounteringType;
        }

        private int encounterCount;
        public Func<PK8, Species, bool> StopCondition { private get; set; } = (pkm, StopOnSpecies) => StopOnSpecies == Species.None ? pkm.IsShiny : pkm.IsShiny && pkm.Species == (int)StopOnSpecies;

        protected override async Task MainLoop(CancellationToken token)
        {
            Connection.Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            Connection.Log("Starting main EncounterBot loop.");
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EncounterBot)
            {
                var attempts = await StepUntilEncounter(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    continue;

                Connection.Log($"Encounter found after {attempts} attempts! Checking details.");
                var pk = new PK8(await Connection.ReadBytesAsync(0x8D45C648, 0x158, token).ConfigureAwait(false));
                if (pk.Species == 0)
                {
                    Connection.Log("Invalid data detected. Restarting loop.");
                    // add stuff for recovering
                    continue;
                }
                
                encounterCount++;
                Connection.Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                Counts.AddCompletedEncounters();
                if (StopCondition(pk, StopOnSpecies))
                {
                    Connection.Log("Result found! Stopping routine execution; re-start the bot(s) to search again.");
                    return;
                } else if (pk.Ability == 119 || pk.Ability == 107) // pokemon with annouced abilites
                {
                    await Task.Delay(2700, token).ConfigureAwait(false);
                }

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "encounters", pk);
                
                await Task.Delay(4600, token).ConfigureAwait(false);
                while (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                {
                    await Click(B, 400, token).ConfigureAwait(false);
                    await Click(B, 900, token).ConfigureAwait(false);
                    await Click(DUP, 500, token).ConfigureAwait(false);
                    await Click(A, 750, token).ConfigureAwait(false);
                }

                await SetStick(LEFT, 0, -30000, 2500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false);
            }

            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0, CancellationToken.None).ConfigureAwait(false); // reset
        }

        private async Task<int> StepUntilEncounter(CancellationToken token)
        {
            Connection.Log("Walking around until an encounter...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EncounterBot)
            {
                if (await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
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
                } else
                {
                    while (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                    {
                        await Click(B, 400, token).ConfigureAwait(false);
                        await Click(B, 900, token).ConfigureAwait(false);
                        await Click(DUP, 500, token).ConfigureAwait(false);
                        await Click(A, 750, token).ConfigureAwait(false);
                        continue;
                    }
                }

                var data = await Connection.ReadBytesAsync(0x8D45C648, 0x158, token).ConfigureAwait(false);
                var pk = new PK8(data);
                if (pk.Species == 0)
                {
                    continue;
                } else if (!await IsCorrectScreen(CurrentScreen_Overworld, token).ConfigureAwait(false))
                {
                    return attempts;
                }

                attempts++;
                if (attempts % 10 == 0)
                    Connection.Log($"Tried {attempts} times, still no encounters.");
            }

            return -1; // aborted
        }
    }
}