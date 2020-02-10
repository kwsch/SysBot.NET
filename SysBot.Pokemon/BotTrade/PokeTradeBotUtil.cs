using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public static class PokeTradeBotUtil
    {
        public static readonly byte[] EMPTY_EC = new byte[4];
        public static readonly byte[] EMPTY_SLOT = new byte[344];

        /// <summary>
        /// Initializes a <see cref="SysBot"/> connection and starts executing a <see cref="PokeTradeBot"/>.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="queue">Queue to consume from; added to from another thread.</param>
        /// <param name="routineType">Task the Trade Bot will perform</param>
        /// <param name="token">Token to indicate cancellation.</param>
        public static async Task RunBotAsync(string[] lines, PokeTradeHub<PK8> queue, PokeRoutineType routineType, CancellationToken token)
        {
            var bot = CreateNewPokeTradeBot(lines, queue);
            bot.Config.NextRoutineType = routineType;
            await bot.RunAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a <see cref="SysBot"/> but does not start it.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="hub"></param>
        public static PokeTradeBot CreateNewPokeTradeBot(string[] lines, PokeTradeHub<PK8> hub)
        {
            var cfg = SwitchBotConfig.GetConfig<PokeBotConfig>(lines);
            return new PokeTradeBot(hub, cfg);
        }
    }
}