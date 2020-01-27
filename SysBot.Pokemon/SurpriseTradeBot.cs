using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchCommand;
using static SysBot.Base.SwitchButton;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot that launches Surprise Trade and repeatedly trades the same PKM. Dumps all received pkm to a dump folder.
    /// </summary>
    public class SurpriseTradeBot
    {
        private readonly SwitchBot Bot;
        private readonly PokemonPool<PK8> Pool = new PokemonPool<PK8>();
        private const int MyGiftAddress = 0x4293D8B0;
        private const int ReadPartyFormatPokeSize = 0x158;

        public string DumpFolder { get; set; }

        public SurpriseTradeBot(string ip, int port) => Bot = new SwitchBot(ip, port);
        public SurpriseTradeBot(SwitchBotConfig cfg) : this(cfg.IP, cfg.Port) { }

        public void Load(PK8 pk) => Pool.Add(pk);
        public bool LoadFolder(string folder) => Pool.LoadFolder(folder);
        private PK8 GetInjectPokemonData() => Pool.GetRandomPoke();

        /// <summary>
        /// Connects to the console, then runs the bot.
        /// </summary>
        /// <param name="token">Cancel this token to have the bot stop looping.</param>
        public async Task RunAsync(CancellationToken token)
        {
            await Bot.Connect().ConfigureAwait(false);
            await MainLoop(token).ConfigureAwait(false);
            await Bot.Disconnect().ConfigureAwait(false);
        }

        private async Task MainLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Inject to b1s1
                var pkm = GetInjectPokemonData();
                var edata = pkm.EncryptedPartyData;
                await Bot.Send(Poke(MyGiftAddress, edata), token).ConfigureAwait(false);

                // load up y comm
                await Bot.Send(Click(Y), token).ConfigureAwait(false);
                await Task.Delay(1000, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                // navigate to start trade
                await Bot.Send(Click(DDOWN), token).ConfigureAwait(false);
                await Task.Delay(500, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                await Bot.Send(Click(A), token).ConfigureAwait(false);
                await Task.Delay(4000, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                await Bot.Send(Click(A), token).ConfigureAwait(false);
                await Task.Delay(700, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                await Bot.Send(Click(A), token).ConfigureAwait(false);
                await Task.Delay(8000, token).ConfigureAwait(false);
                await Bot.Send(Click(A), token).ConfigureAwait(false);
                await Task.Delay(700, token).ConfigureAwait(false);
                await Bot.Send(Click(A), token).ConfigureAwait(false);
                await Task.Delay(700, token).ConfigureAwait(false);
                await Bot.Send(Click(A), token).ConfigureAwait(false);
                await Task.Delay(700, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                // Time we wait for a trade
                await Task.Delay(4500, token).ConfigureAwait(false);
                await Bot.Send(Click(Y), token).ConfigureAwait(false);
                await Task.Delay(700, token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                await WaitForTradeToFinish(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                    break;

                if (DumpFolder == null)
                    continue; // don't dump, just loop

                // get pokemon from box1slot1
                var data = await Bot.ReadBytes(MyGiftAddress, ReadPartyFormatPokeSize, token).ConfigureAwait(false);
                var pk8 = new PK8(data);
                File.WriteAllBytes(Path.Combine(DumpFolder, Util.CleanFileName(pk8.FileName)), pk8.DecryptedPartyData);
            }
        }

        private static async Task WaitForTradeToFinish(CancellationToken token)
        {
            // probably needs to be longer for trade evolutions
            await Task.Delay(30_000, token).ConfigureAwait(false);
        }
    }
}
