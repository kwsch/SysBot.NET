using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    public class FossilBot : PokeRoutineExecutor
    {
        private readonly BotCompleteCounts Counts;
        private readonly IDumper DumpSetting;
        private readonly FossilSpecies FossilSpecies;
        private readonly FossilSettings Settings;
        private bool CaptureVideo;

        public FossilBot(PokeBotConfig cfg, PokeTradeHub<PK8> Hub) : base(cfg)
        {
            Counts = Hub.Counts;
            DumpSetting = Hub.Config.Folder;
            Settings = Hub.Config.Fossil;
            FossilSpecies = Settings.Species;
            CaptureVideo = Hub.Config.CaptureVideoClip;
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public Func<PK8, bool> StopCondition { private get; set; } = pkm => pkm.IsShiny;

        private static readonly PK8 Blank = new PK8();

        protected override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            var originalTextSpeed = await EnsureTextSpeedFast(token).ConfigureAwait(false);

            Log("Checking destination slot for revived fossil Pokémon...");
            var existing = await GetBoxSlotQuality(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Quality != SlotQuality.Overwritable)
            {
                PrintBadSlotMessage(existing);
                return;
            }

            Log("Checking item counts...");
            var pouchData = await Connection.ReadBytesAsync(PokeDataOffsets.ItemTreasureAddress, 80, token).ConfigureAwait(false);
            var counts = FossilCount.GetFossilCounts(pouchData);
            int reviveCount = counts.PossibleRevives(FossilSpecies);
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
                    Log($"Ran out of fossils to revive {FossilSpecies}.");
                    if (Settings.InjectWhenEmpty)
                    {
                        Log("Restoring original pouch data.");
                        await Connection.WriteBytesAsync(pouchData, PokeDataOffsets.ItemTreasureAddress, token).ConfigureAwait(false);
                        await Task.Delay(500, token).ConfigureAwait(false);
                    }
                    else
                    {
                        Log("Restart the game and the bot(s) or set \"Inject Fossils\" to True in the config.");
                        return;
                    }
                }

                await ReviveFossil(counts, token).ConfigureAwait(false);
                Log("Fossil revived. Checking details...");

                var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
                if (pk.Species == 0 || !pk.ChecksumValid)
                {
                    Log("Invalid data detected in destination slot. Restarting loop.");
                    continue;
                }

                encounterCount++;
                Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "fossil", pk);

                Counts.AddCompletedFossils();

                if (StopCondition(pk))
                {
                    if (CaptureVideo)
                        await PressAndHold(CAPTURE, 2_000, 1_000, token).ConfigureAwait(false);

                    if (Settings.ContinueAfterMatch)
                    {
                        Log("Result found! Continuing to collect more fossils.");
                    }
                    else
                    {
                        Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                        return;
                    }
                }

                Log("Clearing destination slot.");
                await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);
            }
            await SetTextSpeed(originalTextSpeed, token).ConfigureAwait(false);
        }

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
            if (count.UseSecondOption1(FossilSpecies))
                await Click(DDOWN, 0_300, token).ConfigureAwait(false);
            await Click(A, 1_300, token).ConfigureAwait(false);

            // Selecting second fossil.
            if (count.UseSecondOption2(FossilSpecies))
                await Click(DDOWN, 300, token).ConfigureAwait(false);

            // A spam through accepting the fossil and agreeing to revive.
            for (int i = 0; i < 8; i++)
                await Click(A, 0_400, token).ConfigureAwait(false);

            // Safe to mash B from here until we get out of all menus.
            while (!await IsOnOverworldFossil(token).ConfigureAwait(false))
                await Click(B, 0_400, token).ConfigureAwait(false);
        }
    }
}
