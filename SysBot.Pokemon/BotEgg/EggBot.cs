using PKHeX.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;

namespace SysBot.Pokemon
{
    public class EggBot : PokeRoutineExecutor
    {
        private readonly PokeTradeHub<PK8> hub;
        private readonly BotCompleteCounts Counts;
        private readonly Nature DesiredNature;
        private readonly int[] DesiredIVs = { -1, -1, -1, -1, -1, -1 };
        private readonly IDumper DumpSetting;
        private readonly bool ContinueGettingEggs;
        
        private const SwordShieldDaycare Location = SwordShieldDaycare.Route5;

        public EggBot(PokeBotConfig cfg, PokeTradeHub<PK8> Hub) : base(cfg)
        {
            hub = Hub;
            Counts = Hub.Counts;
            DumpSetting = Hub.Config.Folder;
            ContinueGettingEggs = Hub.Config.Egg.ContinueAfterMatch;
            AnyRandomEgg = Hub.Config.Egg.AnyRandomEgg;
            
            DesiredNature = Hub.Config.Egg.DesiredNature;

            /* Populate DesiredIVs array.  Bot matches 0 and 31 IVs.
             * Any other nonzero IV is treated as a minimum accepted value.
             * If they put "x", this is a wild card so we can leave -1. */
            string[] splitIVs = Hub.Config.Egg.DesiredIVs.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // Only accept up to 6 values in case people can't count.
            for (int i = 0; i < 6 && i < splitIVs.Length; i++)
            {
                if (splitIVs[i] == "x" || splitIVs[i] == "X")
                    continue;
                DesiredIVs[i] = Convert.ToInt32(splitIVs[i]);
            }
        }

        private int encounterCount;

        private const int InjectBox = 0;
        private const int InjectSlot = 0;
        
        private bool StopCondition(PK8 pk)
        {
            /*  Match species and look for a shiny. This is only checked for walking encounters.
             *  If they specified a species, it has to match. */
             
            if(AnyRandomEgg){
            
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
            
            if(pk.IsShiny){
            
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
                
            }
       
            return true;
        
        }

        protected override async Task MainLoop(CancellationToken token)
        {
            Log("Identifying trainer data of the host console.");
            await IdentifyTrainer(token).ConfigureAwait(false);

            await SetCurrentBox(0, token).ConfigureAwait(false);

            Log("Checking destination slot...");
            var existing = await GetBoxSlotQuality(InjectBox, InjectSlot, token).ConfigureAwait(false);
            if (existing.Quality != SlotQuality.Overwritable)
            {
                PrintBadSlotMessage(existing);
                return;
            }

            Log("Starting main EggBot loop.");
            Config.IterateNextRoutine();
            var blank = new PK8();
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                // Walk a step left, then right => check if egg was generated on this attempt.
                // Repeat until an egg is generated.

                var attempts = await StepUntilEgg(token).ConfigureAwait(false);
                if (attempts < 0) // aborted
                    continue;

                Log($"Egg available after {attempts} attempts! Clearing destination slot.");
                await SetBoxPokemon(blank, InjectBox, InjectSlot, token).ConfigureAwait(false);

                for (int i = 0; i < 4; i++)
                    await Click(A, 0_400, token).ConfigureAwait(false);

                // Safe to mash B from here until we get out of all menus.
                while (!await IsOnOverworld(hub.Config, token).ConfigureAwait(false))
                    await Click(B, 0_400, token).ConfigureAwait(false);

                Log("Egg received. Checking details.");
                var pk = await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false);
                if (pk.Species == 0)
                {
                    Log("Invalid data detected in destination slot. Restarting loop.");
                    continue;
                }

                encounterCount++;
                Log($"Encounter: {encounterCount}:{Environment.NewLine}{ShowdownSet.GetShowdownText(pk)}{Environment.NewLine}{Environment.NewLine}");
                Counts.AddCompletedEggs();

                if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
                    DumpPokemon(DumpSetting.DumpFolder, "egg", pk);

                if (StopCondition(pk))
                {
                    if (ContinueGettingEggs)
                    {
                        Log("Result found! Continuing to collect more eggs.");
                        continue;
                    }
                    Log("Result found! Stopping routine execution; restart the bot(s) to search again.");
                    return;
                }
            }

            // If aborting the sequence, we might have the stick set at some position. Clear it just in case.
            await SetStick(LEFT, 0, 0, 0, CancellationToken.None).ConfigureAwait(false); // reset
        }

        private async Task<int> StepUntilEgg(CancellationToken token)
        {
            Log("Walking around until an egg is ready...");
            int attempts = 0;
            while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.EggFetch)
            {
                await SetEggStepCounter(Location, token).ConfigureAwait(false);

                // Walk Diagonally Left
                await SetStick(LEFT, -19000, 19000, 0_500, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                // Walk Diagonally Right, slightly longer to ensure we stay at the Daycare lady.
                await SetStick(LEFT, 19000, 19000, 0_550, token).ConfigureAwait(false);
                await SetStick(LEFT, 0, 0, 500, token).ConfigureAwait(false); // reset

                bool eggReady = await IsEggReady(Location, token).ConfigureAwait(false);
                if (eggReady)
                    return attempts;

                attempts++;
                if (attempts % 10 == 0)
                    Log($"Tried {attempts} times, still no egg.");

                if (attempts > 10)
                    await Click(B, 500, token).ConfigureAwait(false);
            }

            return -1; // aborted
        }
    }
}
