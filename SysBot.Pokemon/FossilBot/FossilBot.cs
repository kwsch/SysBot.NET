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
        public readonly IDumper DumpSetting;
        public readonly FossilSpecies FossilSpecies;
        public readonly bool InjectFossils;

        public FossilBot(PokeTradeHub<PK8> hub, PokeBotConfig cfg) : base(cfg)
        {
            Counts = hub.Counts;
            DumpSetting = hub.Config;
            FossilSpecies = hub.Config.FossilSpecies;
            InjectFossils = hub.Config.InjectFossils;
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        public Func<PK8, bool> StopCondition { private get; set; } = pkm => pkm.IsShiny;

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

            Connection.Log("Starting main FossilBot loop.");
            var blank = new PK8();
            uint[,] fossilPieces;
            uint possibleRevives;
            
            if (InjectFossils)
            {
                fossilPieces = await RetrieveFossilQuantity(token).ConfigureAwait(false);
                InjectFossilPieces(token, fossilPieces);
            }
            fossilPieces = await RetrieveFossilQuantity(token).ConfigureAwait(false);
            possibleRevives = PossibleRevives(fossilPieces);

            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.FossilBot)
            {

                // Top Half: Select Down for fish if species type == Dracovish || Arctovish
                // Bottom Half: Select Down for dino if species type == Arctozolt || Arctovish
                int[] timings = new int[] { 1100, 1300, 1300, 1200, 1200, 4000, 1200, 1200, 1200, 4500 };

                for (int j = 0; j < timings.Length; j++)
                {
                    // if bird = 0
                    if (j == 2 && fossilPieces[0, 0] != 0)
                    {
                        if (FossilSpecies.ToString() == "Dracovish" || FossilSpecies.ToString() == "Arctovish")
                        {
                            await Click(DDOWN, 300, token).ConfigureAwait(false);
                        }
                    } else if (j == 3 && fossilPieces[2, 0] != 0) // if drake  = 0
                    {
                        if (FossilSpecies.ToString() == "Arctozolt" || FossilSpecies.ToString() == "Arctovish")
                        {
                            await Click(DDOWN, 300, token).ConfigureAwait(false);
                        }
                    }

                    await Click(A, timings[j], token).ConfigureAwait(false);
                }

                Connection.Log("Getting fossil! Clearing destination slot.");
                await SetBoxPokemon(blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

                await Click(A, 2400, token).ConfigureAwait(false);
                await Click(A, 1800, token).ConfigureAwait(false);
                
                Connection.Log("Fossil obtained. Checking details.");
                var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Connection.Log("Invalid data detected in destination slot. Restarting loop.");
                    continue;
                }
                Connection.Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                
                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, pk);

                Counts.AddCompletedFossils();
                encounterCount++;

                if (!StopCondition(pk))
                    continue;

                if (possibleRevives == encounterCount - 1)
                {
                    if (InjectFossils)
                    {
                        Connection.Log($"Ran out of fossils to revive {FossilSpecies.ToString()}. Injecting more fossil pieces.");
                        InjectFossilPieces(token, fossilPieces);
                        break;
                    }
                    Connection.Log($"Ran out of fossils to revive {FossilSpecies.ToString()}. Re-start the game then re-start the bot(s), or set \"Inject Fossils\" to True in the config.");
                    break;
                }

                Connection.Log("Result found! Stopping routine execution; re-start the bot(s) to search again.");
                break;
            }
        }
        private async Task<uint[,]> RetrieveFossilQuantity(CancellationToken token)
        {
            // indexes of items 1105: Fossilized Bird / 1106: Fossilized Fish / 1107: Fossilized Drake / 1108: Fossilized Dino
            uint itemAddress = 0x429358A0;
            var itemsBlock = await Connection.ReadBytesAsync(itemAddress, 100, token).ConfigureAwait(false);
            uint[,] fossils = new uint[4, 2] { { 1, 2 }, { 3, 4 }, { 5, 6 }, { 7, 8 } };

            //uint birdFossil = 0;
            //uint fishFossil = 0;
            //uint drakeFossil = 0;
            //uint dinoFossil = 0;

            for (uint i = 0; i < 25; i++)
            {
                UInt32 item = BitConverter.ToUInt32(itemsBlock, (int)i * 4);
                uint itemIndex = item & 0x7FF;
                uint itemCount = item >> 15 & 0x3FF;

                fossils[0, 0] = itemIndex == 1105 ? itemCount : fossils[0, 0];
                fossils[1, 0] = itemIndex == 1106 ? itemCount : fossils[1, 0];
                fossils[2, 0] = itemIndex == 1107 ? itemCount : fossils[2, 0];
                fossils[3, 0] = itemIndex == 1108 ? itemCount : fossils[3, 0];

                switch (itemIndex)
                {
                    case 1105:
                        fossils[0, 1] = itemAddress + i * 4;
                        break;
                    case 1106:
                        fossils[1, 1] = itemAddress + i * 4;
                        break;
                    case 1107:
                        fossils[2, 1] = itemAddress + i * 4;
                        break;
                    case 1108:
                        fossils[3, 1] = itemAddress + i * 4;
                        break;
                }
            }
            return fossils;
        }
        private uint PossibleRevives(uint[,] fossils)
        {
            uint possibleRevives = 0;
            // birdFossil = 0
            // fishFossil = 1
            // drakeFossil = 2
            // dinoFossil = 3

            // dracozolt = 0
            // arctozolt = 1
            // dracovish = 2
            // arctovish = 3
            String selectedFossil = FossilSpecies.ToString();
            switch (selectedFossil)
            {
                case "Dracozolt":
                    possibleRevives = Math.Min(fossils[0, 0], fossils[2, 0]);
                    break;
                case "Arctozolt":
                    possibleRevives = Math.Min(fossils[0, 0], fossils[3, 0]);
                    break;
                case "Dracovish":
                    possibleRevives = Math.Min(fossils[1, 0], fossils[2, 0]);
                    break;
                case "Arctovish":
                    possibleRevives = Math.Min(fossils[1, 0], fossils[3, 0]);
                    break;
            }
            return possibleRevives;
        }
        private async void InjectFossilPieces(CancellationToken token, uint[,] fossilPieces)
        {
            String selectedFossil = FossilSpecies.ToString();
            switch (selectedFossil)
            {
                case "Dracozolt":
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736337), fossilPieces[0, 1], token).ConfigureAwait(false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736339), fossilPieces[2, 1], token).ConfigureAwait(false);
                    break;
                case "Arctozolt":
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736337), fossilPieces[0, 1], token).ConfigureAwait(false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736340), fossilPieces[3, 1], token).ConfigureAwait(false);
                    break;
                case "Dracovish":
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736338), fossilPieces[1, 1], token).ConfigureAwait(false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736339), fossilPieces[2, 1], token).ConfigureAwait(false);
                    break;
                case "Arctovish":
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736338), fossilPieces[1, 1], token).ConfigureAwait(false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                    await Connection.WriteBytesAsync(BitConverter.GetBytes(32736340), fossilPieces[3, 1], token).ConfigureAwait(false);
                    break;
            }
            Connection.Log("999 of required fossil pieces injected.");
        }
    }
}
