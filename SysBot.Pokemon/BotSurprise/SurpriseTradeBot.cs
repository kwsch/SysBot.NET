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

        public string? DumpFolder { get; set; }

        public SurpriseTradeBot(string ip, int port) : base(ip, port) { }
        public SurpriseTradeBot(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        public void Load(PK8 pk) => Pool.Add(pk);
        public bool LoadFolder(string folder) => Pool.LoadFolder(folder);
        private PK8 GetInjectPokemonData() => Pool.GetRandomPoke();
        
        protected override async Task MainLoop(CancellationToken token)
        {
            // Initialize bot information
            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);
            Connection.Name = $"{sav.OT}-{sav.DisplayTID}";

            while (!token.IsCancellationRequested)
            {
                // Inject to b1s1
                Connection.Log("Starting next trade. Getting data...");
                var pkm = GetInjectPokemonData();
                await Connection.WriteBytesAsync(pkm.EncryptedPartyData, Box1Slot1, token).ConfigureAwait(false);

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
                await ReadDumpB1S1(DumpFolder, token).ConfigureAwait(false);
            }
        }

        private async Task Recover(CancellationToken token)
        {
            for (int i = 0; i < 3; i++)
                await Click(B, 1000, token).ConfigureAwait(false);
        }

        private static async Task WaitForTradeToFinish(CancellationToken token)
        {
            // probably needs to be longer for trade evolutions
            await Task.Delay(30_000, token).ConfigureAwait(false);
        }
    }
}
