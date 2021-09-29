﻿using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public abstract class EncounterBot : PokeRoutineExecutor8, IEncounterBot
    {
        protected readonly PokeTradeHub<PK8> Hub;
        private readonly IDumper DumpSetting;
        private readonly EncounterSettings Settings;
        private readonly int[] DesiredMinIVs;
        private readonly int[] DesiredMaxIVs;
        protected readonly byte[] BattleMenuReady = { 0, 0, 0, 255 };
        public ICountSettings Counts => Settings;

        protected EncounterBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
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
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            await InitializeHardware(settings, token).ConfigureAwait(false);

            try
            {
                Log($"Starting main {GetType().Name} loop.");
                Config.IterateNextRoutine();

                // Clear out any residual stick weirdness.
                await ResetStick(token).ConfigureAwait(false);
                await EncounterLoop(sav, token).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Log(e.Message);
            }

            Log($"Ending {GetType().Name} loop.");
            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            await ResetStick(CancellationToken.None).ConfigureAwait(false);
            await CleanExit(Settings, CancellationToken.None).ConfigureAwait(false);
        }

        protected abstract Task EncounterLoop(SAV8SWSH sav, CancellationToken token);

        // return true if breaking loop
        protected async Task<bool> HandleEncounter(PK8 pk, CancellationToken token)
        {
            encounterCount++;
            var print = Hub.Config.StopConditions.GetPrintName(pk);
            Log($"Encounter: {encounterCount}{Environment.NewLine}{print}{Environment.NewLine}");

            var legendary = Legal.Legends.Contains(pk.Species) || Legal.SubLegends.Contains(pk.Species);
            if (legendary)
                Settings.AddCompletedLegends();
            else
                Settings.AddCompletedEncounters();

            if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                DumpPokemon(DumpSetting.DumpFolder, legendary ? "legends" : "encounters", pk);

            if (!StopConditionSettings.EncounterFound(pk, DesiredMinIVs, DesiredMaxIVs, Hub.Config.StopConditions))
                return false;

            if (Hub.Config.StopConditions.CaptureVideoClip)
            {
                await Task.Delay(Hub.Config.StopConditions.ExtraTimeWaitCaptureVideo, token).ConfigureAwait(false);
                await PressAndHold(CAPTURE, 2_000, 1_000, token).ConfigureAwait(false);
            }

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
                return true;
            if (mode == ContinueAfterMatch.Continue)
                return false;

            IsWaiting = true;
            while (IsWaiting)
                await Task.Delay(1_000, token).ConfigureAwait(false);
            return false;
        }

        private bool IsWaiting;
        public void Acknowledge() => IsWaiting = false;

        protected async Task ResetStick(CancellationToken token)
        {
            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0_500, token).ConfigureAwait(false); // reset
        }

        protected async Task FleeToOverworld(CancellationToken token)
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
