using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using PKHeX.Core.Searching;
using static SysBot.Base.SwitchButton;
using static SysBot.Base.SwitchStick;
using static SysBot.Pokemon.PokeDataOffsets;

namespace SysBot.Pokemon
{
    public sealed class EncounterBotReset : EncounterBot
    {
        public EncounterBotReset(PokeBotState cfg, PokeTradeHub<PK8> hub) : base(cfg, hub)
        {
        }

        protected override async Task EncounterLoop(SAV8SWSH sav, CancellationToken token)
        {
            var monoffset = GetResetOffset(Hub.Config.EncounterSWSH.EncounteringType);
            var pkoriginal = monoffset is BoxStartOffset ? await ReadBoxPokemon(0, 0, token).ConfigureAwait(false) : new PK8();

            while (!token.IsCancellationRequested)
            {
                PK8? pknew;

                Log("Looking for a Pokémon...");
                do
                {
                    await DoExtraCommands(token, Hub.Config.EncounterSWSH.EncounteringType).ConfigureAwait(false);
                    pknew = await ReadUntilPresent(monoffset, 0_050, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
                } while (pknew is null || SearchUtil.HashByDetails(pkoriginal) == SearchUtil.HashByDetails(pknew));

                if (await HandleEncounter(pknew, token).ConfigureAwait(false))
                    return;

                Log("No match, resetting the game...");
                await CloseGame(Hub.Config, token).ConfigureAwait(false);
                await StartGame(Hub.Config, token).ConfigureAwait(false);
            }
        }

        private async Task DoExtraCommands(CancellationToken token, EncounterMode mode)
        {
            switch (mode)
            {
                case EncounterMode.Eternatus or EncounterMode.MotostokeGym:
                    await SetStick(LEFT, 0, 20_000, 0_500, token).ConfigureAwait(false);
                    await ResetStick(token).ConfigureAwait(false);
                    break;
                default:
                    await Click(A, 0_050, token).ConfigureAwait(false);
                    break;
            }
        }

        private static uint GetResetOffset(EncounterMode mode) => mode switch
        {
            EncounterMode.Gift                                 => BoxStartOffset,
            EncounterMode.Regigigas or EncounterMode.Eternatus => RaidPokemonOffset,
            EncounterMode.MotostokeGym                         => LegendaryPokemonOffset,
            _ => WildPokemonOffset,
        };
    }
}
