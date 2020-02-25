﻿using PKHeX.Core;
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
        private readonly PokeTradeHubConfig Settings;

        public FossilBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Counts = hub.Counts;
            DumpSetting = hub.Config;
            FossilSpecies = hub.Config.FossilSpecies;
            Settings = hub.Config;
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public Func<PK8, bool> StopCondition { private get; set; } = pkm => pkm.IsShiny;

        private static readonly PK8 Blank = new PK8();

        protected override async Task MainLoop(CancellationToken token)
        {
            Connection.Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            Connection.Log("Checking destination slot for revived fossil Pokemon to see if anything is in the slot...");
            var existing = await GetBoxSlotQuality(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Quality != SlotQuality.Overwritable)
            {
                PrintBadSlotMessage(existing);
                return;
            }

            Connection.Log("Checking item counts...");
            var pouchData = await Connection.ReadBytesAsync(PokeDataOffsets.ItemTreasureAddress, 80, token).ConfigureAwait(false);
            var counts = FossilCount.GetFossilCounts(pouchData);
            int reviveCount = counts.PossibleRevives(FossilSpecies);
            if (reviveCount == 0)
            {
                Connection.Log("Insufficient fossil pieces to start. Please obtain at least one of each required fossil piece before starting.");
                return;
            }

            Connection.Log("Starting main FossilBot loop.");
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.FossilBot)
            {
                await ReviveFossil(counts, token).ConfigureAwait(false);
                Connection.Log("Fossil revived. Checking details.");

                var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
                if (pk.Species == 0 || !pk.ChecksumValid)
                {
                    Connection.Log("Invalid data detected in destination slot. Restarting loop.");
                    continue;
                }

                encounterCount++;
                Connection.Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                if (DumpSetting.Dump)
                    DumpPokemon(DumpSetting.DumpFolder, "fossil", pk);

                Counts.AddCompletedFossils();

                if (StopCondition(pk))
                {
                    Connection.Log("Result found! Stopping routine execution; re-start the bot(s) to search again.");
                    return;
                }

                if (encounterCount % reviveCount != 0)
                    continue;

                Connection.Log($"Ran out of fossils to revive {FossilSpecies}.");
                if (Settings.InjectFossils)
                {
                    Connection.Log("Restoring original pouch data.");
                    await Connection.WriteBytesAsync(pouchData, PokeDataOffsets.ItemTreasureAddress, token).ConfigureAwait(false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                }
                else
                {
                    Connection.Log("Re-start the game then re-start the bot(s), or set \"Inject Fossils\" to True in the config.");
                    return;
                }
            }
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
            await Click(A, 1200, token).ConfigureAwait(false);
            await Click(A, 4500, token).ConfigureAwait(false);

            Connection.Log("Getting fossil! Clearing destination slot.");
            await SetBoxPokemon(Blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

            await Click(A, 2400, token).ConfigureAwait(false);
            await Click(A, 1800, token).ConfigureAwait(false);
        }
    }
}
