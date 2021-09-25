using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class FossilBot : PokeRoutineExecutor8, IEncounterBot
    {
        private readonly PokeTradeHub<PK8> Hub;
        private readonly FossilSettings Settings;
        private readonly IDumper DumpSetting;
        private readonly int[] DesiredMinIVs;
        private readonly int[] DesiredMaxIVs;
        public ICountSettings Counts => Settings;

        public FossilBot(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg)
        {
            Hub = hub;
            Settings = Hub.Config.Fossil;
            DumpSetting = Hub.Config.Folder;
            StopConditionSettings.InitializeTargetIVs(Hub, out DesiredMinIVs, out DesiredMaxIVs);
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        private static readonly PK8 Blank = new();

        public override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);
            await InitializeHardware(Settings, token).ConfigureAwait(false);
            await SetCurrentBox(0, token).ConfigureAwait(false);

            var existing = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Species != 0 && existing.ChecksumValid)
            {
                Log("Destination slot is occupied! Dumping the Pokémon found there...");
                DumpPokemon(DumpSetting.DumpFolder, "saved", existing);
            }
            Log("Clearing destination slot to start the bot.");
            await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

            Log("Checking item counts...");
            var pouchData = await Connection.ReadBytesAsync(ItemTreasureAddress, 80, token).ConfigureAwait(false);
            var counts = FossilCount.GetFossilCounts(pouchData);
            int reviveCount = counts.PossibleRevives(Settings.Species);
            if (reviveCount == 0)
            {
                Log("Insufficient fossil pieces. Please obtain at least one of each required fossil piece first.");
                return;
            }

            Log("Starting main FossilBot loop.");
            Config.IterateNextRoutine();
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.FossilBot)
            {
                if (encounterCount != 0 && encounterCount % reviveCount == 0)
                {
                    Log($"Ran out of fossils to revive {Settings.Species}.");
                    if (Settings.InjectWhenEmpty)
                    {
                        Log("Restoring original pouch data.");
                        await Connection.WriteBytesAsync(pouchData, ItemTreasureAddress, token).ConfigureAwait(false);
                        await Task.Delay(500, token).ConfigureAwait(false);
                    }
                    else
                    {
                        Log("Restart the game and the bot(s) or set \"Inject Fossils\" to True in the config.");
                        return;
                    }
                }

                Log("Clearing destination slot.");
                await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);
                await ReviveFossil(counts, token).ConfigureAwait(false);
                Log("Fossil revived. Checking details...");

                var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
                if (pk.Species == 0 || !pk.ChecksumValid)
                {
                    Log("Invalid data detected in destination slot. Restarting loop.");
                    continue;
                }

                encounterCount++;
                var print = Hub.Config.StopConditions.GetPrintName(pk);
                Log($"Encounter: {encounterCount}{Environment.NewLine}{print}{Environment.NewLine}");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "fossil", pk);

                Settings.AddCompletedFossils();

                if (!StopConditionSettings.EncounterFound(pk, DesiredMinIVs, DesiredMaxIVs, Hub.Config.StopConditions))
                    continue;

                if (Hub.Config.StopConditions.CaptureVideoClip)
                {
                    await Task.Delay(Hub.Config.StopConditions.ExtraTimeWaitCaptureVideo, token).ConfigureAwait(false);
                    await PressAndHold(CAPTURE, 2_000, 1_000, token).ConfigureAwait(false);
                }

                var mode = Settings.ContinueAfterMatch;
                var msg = mode switch
                {
                    ContinueAfterMatch.Continue => $"Result found!\n{print}\nContinuing...",
                    ContinueAfterMatch.PauseWaitAcknowledge => $"Result found!\n{print}\nI'm waiting for you to acknowledge via command to continue.",
                    ContinueAfterMatch.StopExit => $"Result found!\n{print}\nStopping routine execution; restart the bot(s) to search again.",
                    _ => throw new ArgumentOutOfRangeException(),
                };

                if (!string.IsNullOrWhiteSpace(Hub.Config.StopConditions.MatchFoundEchoMention))
                    msg = $"{Hub.Config.StopConditions.MatchFoundEchoMention} {msg}";
                EchoUtil.Echo(msg);
                Log(msg);

                if (mode == ContinueAfterMatch.StopExit)
                    break;
                if (mode == ContinueAfterMatch.Continue)
                    continue;

                IsWaiting = true;
                while (IsWaiting)
                    await Task.Delay(1_000, token).ConfigureAwait(false);
            }

            await HardStop().ConfigureAwait(false);
        }

        public override async Task HardStop()
        {
            await CleanExit(Settings, CancellationToken.None).ConfigureAwait(false);
        }

        private bool IsWaiting;
        public void Acknowledge() => IsWaiting = false;

        private async Task ReviveFossil(FossilCount count, CancellationToken token)
        {
            Log("Starting fossil revival routine...");
            if (GameLang == LanguageID.Spanish)
                await Click(A, 0_900, token).ConfigureAwait(false);

            await Click(A, 1_100, token).ConfigureAwait(false);

            // French is slightly slower.
            if (GameLang == LanguageID.French)
                await Task.Delay(0_200, token).ConfigureAwait(false);

            await Click(A, 1_300, token).ConfigureAwait(false);

            // Selecting first fossil.
            if (count.UseSecondOption1(Settings.Species))
                await Click(DDOWN, 0_300, token).ConfigureAwait(false);
            await Click(A, 1_300, token).ConfigureAwait(false);

            // Selecting second fossil.
            if (count.UseSecondOption2(Settings.Species))
                await Click(DDOWN, 300, token).ConfigureAwait(false);

            // A spam through accepting the fossil and agreeing to revive.
            for (int i = 0; i < 8; i++)
                await Click(A, 0_400, token).ConfigureAwait(false);

            // Safe to mash B from here until we get out of all menus.
            while (!await IsOnOverworld(Hub.Config, token).ConfigureAwait(false))
                await Click(B, 0_400, token).ConfigureAwait(false);
        }
    }
}
