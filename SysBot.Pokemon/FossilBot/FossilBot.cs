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

        public FossilBot(PokeBotConfig cfg, FossilSettings fossil, IDumper dump, BotCompleteCounts count) : base(cfg)
        {
            Counts = count;
            DumpSetting = dump;
            Settings = fossil;
            FossilSpecies = fossil.Species;
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

            Log("Checking destination slot for revived fossil Pokémon to see if anything is in the slot...");
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
                Log("Insufficient fossil pieces to start. Please obtain at least one of each required fossil piece before starting.");
                return;
            }

            Log("Starting main FossilBot loop.");
            Config.IterateNextRoutine();
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.FossilBot)
            {
                await ReviveFossil(counts, token).ConfigureAwait(false);
                Log("Fossil revived. Checking details.");

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
                    if (Settings.ContinueAfterMatch)
                    {
                        Log("Result found! Continuing to collect more fossils.");
                        continue;
                    }
                    Log("Result found! Stopping routine execution; re-start the bot(s) to search again.");
                    return;
                }

                if (encounterCount % reviveCount != 0)
                    continue;

                Log($"Ran out of fossils to revive {FossilSpecies}.");
                if (Settings.InjectWhenEmpty)
                {
                    Log("Restoring original pouch data.");
                    await Connection.WriteBytesAsync(pouchData, PokeDataOffsets.ItemTreasureAddress, token).ConfigureAwait(false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                }
                else
                {
                    Log("Re-start the game then re-start the bot(s), or set \"Inject Fossils\" to True in the config.");
                    return;
                }
            }
            await SetTextSpeed(originalTextSpeed, token).ConfigureAwait(false);
        }

        private async Task ReviveFossil(FossilCount count, CancellationToken token)
        {
            await Click(A, 1100, token).ConfigureAwait(false);
            await Click(A, 1300, token).ConfigureAwait(false);

            if (count.UseSecondOption1(FossilSpecies))
                await Click(DDOWN, 300, token).ConfigureAwait(false);
            await Click(A, 1300, token).ConfigureAwait(false);

            if (count.UseSecondOption2(FossilSpecies))
                await Click(DDOWN, 300, token).ConfigureAwait(false);
            await Click(A, 1200, token).ConfigureAwait(false);
            await Click(A, 1200, token).ConfigureAwait(false);

            await Click(A, 4000, token).ConfigureAwait(false);
            await Click(A, 1200, token).ConfigureAwait(false);
            await Click(A, 1200, token).ConfigureAwait(false);
            if (GameLang == LanguageID.French)
                await Click(A, 800, token).ConfigureAwait(false);
            await Click(A, 1200, token).ConfigureAwait(false);
            await Click(A, 4500, token).ConfigureAwait(false);

            Log("Getting fossil! Clearing destination slot.");
            await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

            await Click(A, 2400, token).ConfigureAwait(false);
            await Click(A, 1800, token).ConfigureAwait(false);
        }
    }
}
