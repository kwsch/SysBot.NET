using System.Threading;
using System.Threading.Tasks;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public static class EggBotUtil
    {
        /// <summary>
        /// Initializes a <see cref="SysBot"/> connection and starts executing a <see cref="EggBot"/>.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        /// <param name="token">Token to indicate cancellation.</param>
        public static async Task RunBotAsync(string[] lines, CancellationToken token)
        {
            var bot = CreateNewEggBot(lines);
            await bot.RunAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes a <see cref="SysBot"/> but does not start it.
        /// </summary>
        /// <param name="lines">Lines to initialize with</param>
        public static EggBot CreateNewEggBot(string[] lines)
        {
            var cfg = SwitchBotConfig.GetConfig<PokeBotConfig>(lines);
            return new EggBot(cfg);
        }
    }
}