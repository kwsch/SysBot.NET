using System;

namespace SysBot.Base
{
    /// <summary>
    /// List of bots saved in the config.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BotList<T> where T : IConsoleBotConfig
    {
#pragma warning disable CA1819 // Properties should not return arrays
        /// <summary> Saved bots </summary>
        public T[] Bots { get; set; } = Array.Empty<T>();
#pragma warning restore CA1819 // Properties should not return arrays
    }
}