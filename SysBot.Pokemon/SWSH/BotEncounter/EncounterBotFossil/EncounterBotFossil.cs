using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public class EncounterBotFossil : EncounterBot
    {
        private readonly FossilSettings Settings;
        private readonly IDumper DumpSetting;

        public EncounterBotFossil(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg, hub)
        {
            Settings = Hub.Config.EncounterSWSH.Fossil;
            DumpSetting = Hub.Config.Folder;
        }

        private static readonly PK8 Blank = new();

        protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
        {
            await SetupBoxState(DumpSetting, token).ConfigureAwait(false);

            Log("Checking item counts...");
            var pouchData = await Connection.ReadBytesAsync(ItemTreasureAddress, 80, token).ConfigureAwait(false);
            var counts = FossilCount.GetFossilCounts(pouchData);
            int reviveCount = counts.PossibleRevives(Settings.Species);
            if (reviveCount == 0)
            {
                Log("Insufficient fossil pieces. Please obtain at least one of each required fossil piece first.");
                return;
            }
            Log($"Enough fossil pieces are available to revive {reviveCount} {Settings.Species}.");

            while (!token.IsCancellationRequested)
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
                        Log("Fossil pieces have been depleted. Resetting the game.");
                        await CloseGame(Hub.Config, token).ConfigureAwait(false);
                        await StartGame(Hub.Config, token).ConfigureAwait(false);
                        await SetupBoxState(DumpSetting, token).ConfigureAwait(false);
                    }
                }

                await ReviveFossil(counts, token).ConfigureAwait(false);
                Log("Fossil revived. Checking details...");

                var pk = await ReadBoxPokemon(0, 0, token).ConfigureAwait(false);
                if (pk.Species == 0 || !pk.ChecksumValid)
                {
                    Log("No fossil found in Box 1, slot 1. Ensure that the party is full. Restarting loop.");
                    continue;
                }

                if (await HandleEncounter(pk, token).ConfigureAwait(false))
                    return;

                Log("Clearing destination slot.");
                await SetBoxPokemon(Blank, 0, 0, token).ConfigureAwait(false);
            }
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
            while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                await Click(B, 0_400, token).ConfigureAwait(false);
        }
    }
}
