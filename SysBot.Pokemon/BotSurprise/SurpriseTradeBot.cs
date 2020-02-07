using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot that launches Surprise Trade and repeatedly trades the same PKM. Dumps all received pkm to a dump folder.
    /// </summary>
    public class SurpriseTradeBot : PokeRoutineExecutor
    {
        public readonly PokemonPool<PK8> Pool = new PokemonPool<PK8>();

        /// <summary>
        /// Folder to dump received trade data to.
        /// </summary>
        /// <remarks>If null, will skip dumping.</remarks>
        public string? DumpFolder { get; set; }

        public SurpriseTradeBot(string ip, int port) : base(ip, port) { }
        public SurpriseTradeBot(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        private PK8 GetInjectPokemonData() => Pool.GetRandomPoke();

        private const int InjectBox = 0;
        private const int InjectSlot = 0;

        protected override async Task MainLoop(CancellationToken token)
        {
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);

            while (!token.IsCancellationRequested)
            {
                // Inject to b1s1
                Connection.Log("Starting next trade. Getting data...");
                var pkm = GetInjectPokemonData();
                await SetBoxPokemon(pkm, InjectBox, InjectSlot, token, sav).ConfigureAwait(false);

                if (!await IsGameConnectedToYCom(token).ConfigureAwait(false))
                {
                    Connection.Log("Reconnecting to Y-Com...");
                    await ReconnectToYCom(token).ConfigureAwait(false);
                }

                Connection.Log("Open Y-COM Menu");
                await Click(Y, 1_000, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                Connection.Log("Select Surprise Trade");
                await Click(DDOWN, 0_100, token).ConfigureAwait(false);
                await Click(A, 4_000, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                Connection.Log("Select Pokemon");
                // Box 1 Slot 1
                await Click(A, 0_700, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                Connection.Log("Confirming...");
                await Click(A, 8_000, token).ConfigureAwait(false);
                for (int i = 0; i < 3; i++)
                    await Click(A, 0_700, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                // Time we wait for a trade
                await Task.Delay(45_000, token).ConfigureAwait(false);
                await Click(Y, 0_700, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                await WaitForTradeToFinish(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                Connection.Log("Trade complete!");
                if (DumpFolder != null)
                    DumpPokemon(DumpFolder, await ReadBoxPokemon(InjectBox, InjectSlot, token).ConfigureAwait(false));
            }
        }

        private static async Task WaitForTradeToFinish(CancellationToken token)
        {
            // probably needs to be longer for trade evolutions
            await Task.Delay(35_000, token).ConfigureAwait(false);
        }
    }
}
