using System;

namespace SysBot.Base
{
    /// <summary>
    /// List of bots saved in the config.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BotList<T> where T : IConsoleBotConfig
    {
        /// <summary> Saved bots </summary>
        public T[] Bots { get; set; } = Array.Empty<T>();
    }
}