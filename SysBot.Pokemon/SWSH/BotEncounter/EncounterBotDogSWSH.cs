using PKHeX.Core;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsetsSWSH;

namespace SysBot.Pokemon;

public sealed class EncounterBotDogSWSH(PokeBotState Config, PokeTradeHub<PK8> Hub) : EncounterBotSWSH(Config, Hub)
{
    protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
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

            // Get rid of any stick stuff left over, so we can flee properly.
            await ResetStick(token).ConfigureAwait(false);

            // Wait for the entire cutscene.
            await Task.Delay(15_000, token).ConfigureAwait(false);

            while (!await IsOnBattleMenu(token).ConfigureAwait(false))
                await Task.Delay(0_100, token).ConfigureAwait(false);
            await Task.Delay(0_100, token).ConfigureAwait(false);

            if (await HandleEncounter(pk, token).ConfigureAwait(false))
                return;

            Log("Running away...");
            await FleeToOverworld(token).ConfigureAwait(false);

            // Extra delay to be sure we're fully out of the battle.
            await Task.Delay(0_250, token).ConfigureAwait(false);
        }
    }
}
