using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public sealed class EncounterBotDog : EncounterBot
    {
        public EncounterBotDog(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg, hub)
        {
        }

        protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Log("Looking for a new dog...");

                // At the start of each loop, an A press is needed to exit out of a prompt.
                await Click(SwitchButton.A, 0_100, token).ConfigureAwait(false);
                await SetStick(SwitchStick.LEFT, 0, 30000, 1_000, token).ConfigureAwait(false);

                // Encounters Zacian/Zamazenta and clicks through all the menus.
                while (!await IsInBattle(token).ConfigureAwait(false))
                    await Click(SwitchButton.A, 0_300, token).ConfigureAwait(false);

                Log("Encounter started! Checking details...");
                var pk = await ReadUntilPresent(PokeDataOffsets.LegendaryPokemonOffset, 2_000, 0_200, PokeDataOffsets.BoxFormatSlotSize, token).ConfigureAwait(false);
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
                    await ReadUntilChanged(PokeDataOffsets.BattleMenuOffset, BattleMenuReady, 5_000, 0_100, true, token).ConfigureAwait(false);

                if (await HandleEncounter(pk, true, token).ConfigureAwait(false))
                    return;

                Log("Running away...");
                await FleeToOverworld(token).ConfigureAwait(false);

                // Extra delay to be sure we're fully out of the battle.
                await Task.Delay(0_250, token).ConfigureAwait(false);
            }
        }
    }
}
