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
    public class EncounterBot : PokeRoutineExecutor8, ICountBot
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly IDumper DumpSetting;
        private readonly EncounterSettings Settings;
        private readonly int[] DesiredMinIVs;
        private readonly int[] DesiredMaxIVs;
        private readonly byte[] BattleMenuReady = { 0, 0, 0, 255 };
        public ICountSettings Counts => Settings;

        public EncounterBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Settings = Hub.Config.Encounter;
            DumpSetting = Hub.Config.Folder;
            StopConditionSettings.InitializeTargetIVs(Hub, out DesiredMinIVs, out DesiredMaxIVs);
        }

        private int encounterCount;

        public override async Task MainLoop(CancellationToken token)
        {
            var settings = Hub.Config.Encounter;
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);
            await InitializeHardware(settings, token).ConfigureAwait(false);

            Log("Starting main EncounterBot loop.");
            Config.IterateNextRoutine();

            // Clear out any residual stick weirdness.
            await ResetStick(token).ConfigureAwait(false);

            var task = settings.EncounteringType switch
            {
                EncounterMode.VerticalLine => WalkInLine(token),
                EncounterMode.HorizontalLine => WalkInLine(token),
                EncounterMode.Eternatus => DoEternatusEncounter(token),
                EncounterMode.LegendaryDogs => DoDogEncounter(token),
                _ => WalkInLine(token),
            };
            await task.ConfigureAwait(false);

            await ResetStick(token).ConfigureAwait(false);
            await CleanExit(settings, token).ConfigureAwait(false);
        }

        private async Task WalkInLine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var attempts = await StepUntilEncounter(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    continue;

                Log($"Encounter found after {attempts} attempts! Checking details...");

                // Reset stick while we wait for the encounter to load.
                await ResetStick(token).ConfigureAwait(false);

                var pk = await ReadUntilPresent(WildPokemonOffset, 2_000, 0_200, BoxFormatSlotSize, token).ConfigureAwait(false);
                if (pk == null)
                {
                    Log("Invalid data detected. Restarting loop.");

                    // Flee and continue looping.
                    await FleeToOverworld(token).ConfigureAwait(false);
                    continue;
                }

                // Offsets are flickery so make sure we see it 3 times.
                for (int i = 0; i < 3; i++)
                    await ReadUntilChanged(BattleMenuOffset, BattleMenuReady, 5_000, 0_100, true, token).ConfigureAwait(false);

                if (await HandleEncounter(pk, false, token).ConfigureAwait(false))
                    return;

                Log("Running away...");
                await FleeToOverworld(token).ConfigureAwait(false);
            }
        }

        private async Task DoEternatusEncounter(CancellationToken token)
        {
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EncounterBot)
            {
                await SetStick(LEFT, 0, 20_000, 1_000, token).ConfigureAwait(false);
                await ResetStick(token).ConfigureAwait(false);

                var pk = await ReadUntilPresent(RaidPokemonOffset, 2_000, 0_200, BoxFormatSlotSize, token).ConfigureAwait(false);
                if (pk != null)
                {
                    if (await HandleEncounter(pk, true, token).ConfigureAwait(false))
                        return;
                }

                Connection.Log("Resetting Eternatus by restarting the game");
                await CloseGame(Hub.Config, token).ConfigureAwait(false);
                await StartGame(Hub.Config, token).ConfigureAwait(false);
            }
        }

        private async Task DoDogEncounter(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Log("Looking for a new dog...");

                // At the start of each loop, an A press is needed to exit out of a prompt.
                await Click(A, 0_100, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 30000, 1_000, token).ConfigureAwait(false);

                // Encounters Zacian/Zamazenta and clicks through all the menus.
                while (!await IsInBattle(token).ConfigureAwait(false))
                    await Click(A, 0_300, token).ConfigureAwait(false);

                Log("Encounter started! Checking details...");
                var pk = await ReadUntilPresent(LegendaryPokemonOffset, 2_000, 0_200, BoxFormatSlotSize, token).ConfigureAwait(false);
                if (pk == null)
                {
                    Log("Invalid data detected. Restarting loop.");
                    continue;
                }

                // Get rid of any stick stuff left over so we can flee properly.
                await ResetStick(token).ConfigureAwait(false);

                // Wait for the entire cutscene.
                await Task.Delay(15_000, token).ConfigureAwait(false);

                // Offsets are flickery so make sure we see it 3 times.
                for (int i = 0; i < 3; i++)
                    await ReadUntilChanged(BattleMenuOffset, BattleMenuReady, 5_000, 0_100, true, token).ConfigureAwait(false);

                if (await HandleEncounter(pk, true, token).ConfigureAwait(false))
                    return;

                Log("Running away...");
                await FleeToOverworld(token).ConfigureAwait(false);

                // Extra delay to be sure we're fully out of the battle.
                await Task.Delay(0_250, token).ConfigureAwait(false);
            }
        }

        private async Task<int> StepUntilEncounter(CancellationToken token)
        {
            Log("Walking around until an encounter...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EncounterBot)
            {
                if (!await IsInBattle(token).ConfigureAwait(false))
                {
                    switch (Hub.Config.Encounter.EncounteringType)
                    {
                        case EncounterMode.VerticalLine:
                            await SetStick(LEFT, 0, -30000, 2_400, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset

                            // Quit early if we found an encounter on first sweep.
                            if (await IsInBattle(token).ConfigureAwait(false))
                                break;

                            await SetStick(LEFT, 0, 30000, 2_400, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset
                            break;
                        case EncounterMode.HorizontalLine:
                            await SetStick(LEFT, -30000, 0, 2_400, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset

                            // Quit early if we found an encounter on first sweep.
                            if (await IsInBattle(token).ConfigureAwait(false))
                                break;

                            await SetStick(LEFT, 30000, 0, 2_400, token).ConfigureAwait(false);
                            await SetStick(LEFT, 0, 0, 0_100, token).ConfigureAwait(false); // reset
                            break;
                    }

                    attempts++;
                    if (attempts % 10 == 0)
                        Log($"Tried {attempts} times, still no encounters.");
                }

                if (await IsInBattle(token).ConfigureAwait(false))
                    return attempts;
            }

            return -1; // aborted
        }

        private async Task<bool> HandleEncounter(PK8 pk, bool legends, CancellationToken token)
        {
            encounterCount++;
            Log($"Encounter: {encounterCount}{Environment.NewLine}{ShowdownParsing.GetShowdownText(pk)}{Environment.NewLine}");
            if (legends)
                Settings.AddCompletedLegends();
            else
                Settings.AddCompletedEncounters();

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, legends ? "legends" : "encounters", pk);

            if (StopConditionSettings.EncounterFound(pk, DesiredMinIVs, DesiredMaxIVs, Hub.Config.StopConditions))
            {
                const string msg = "Result found! Stopping routine execution; restart the bot(s) to search again.";
                if (!string.IsNullOrWhiteSpace(Hub.Config.StopConditions.MatchFoundEchoMention))
                    EchoUtil.Echo($"{Hub.Config.StopConditions.MatchFoundEchoMention} {msg}");
                Log(msg);

                if (Hub.Config.StopConditions.CaptureVideoClip)
                {
                    await Task.Delay(Hub.Config.StopConditions.ExtraTimeWaitCaptureVideo, token).ConfigureAwait(false);
                    await PressAndHold(CAPTURE, 2_000, 1_000, token).ConfigureAwait(false);
                }
                return true;
            }

            return false;
        }

        private async Task ResetStick(CancellationToken token)
        {
            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0_500, token).ConfigureAwait(false); // reset
        }

        private async Task FleeToOverworld(CancellationToken token)
        {
            // This routine will always escape a battle.
            await Click(DUP, 0_200, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);

            while (await IsInBattle(token).ConfigureAwait(false))
            {
                await Click(B, 0_500, token).ConfigureAwait(false);
                await Click(B, 1_000, token).ConfigureAwait(false);
                await Click(DUP, 0_200, token).ConfigureAwait(false);
                await Click(A, 1_000, token).ConfigureAwait(false);
            }
        }
    }
}
