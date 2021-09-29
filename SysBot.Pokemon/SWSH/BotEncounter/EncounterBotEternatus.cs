using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public sealed class EncounterBotEternatus : EncounterBot
    {
        public EncounterBotEternatus(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg, hub)
        {
        }

        protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await SetStick(LEFT, 0, 20_000, 1_000, token).ConfigureAwait(false);
                await ResetStick(token).ConfigureAwait(false);

                var pk = await ReadUntilPresent(RaidPokemonOffset, 2_000, 0_200, BoxFormatSlotSize, token).ConfigureAwait(false);
                if (pk != null)
                {
                    if (await HandleEncounter(pk, token).ConfigureAwait(false))
                        return;
                }

                Log("No match, resetting the game...");
                await CloseGame(Hub.Config, token).ConfigureAwait(false);
                await StartGame(Hub.Config, token).ConfigureAwait(false);
            }
        }
    }
}
