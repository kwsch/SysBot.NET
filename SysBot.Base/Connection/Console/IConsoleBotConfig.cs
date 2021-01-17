namespace SysBot.Base
{
    /// <summary>
    /// Bare minimum details for a saved configuration object.
    /// </summary>
    public interface IConsoleBotConfig
    {
        /// <summary>
        /// Checks if the <see cref="IConsoleBotConfig"/> is valid or not.
        /// </summary>
        bool IsValid();

        /// <summary>
        /// Checks if the config matches the input <see cref="magic"/>.
        /// </summary>
        /// <param name="magic">Magic value to be used by the comparison to see if it matches.</param>
        bool Matches(string magic);
    }

    /// <summary>
    /// Exposes methods to obtain the communication object that interacts with the console.
    /// </summary>
    /// <typeparam name="TSync"></typeparam>
    /// <typeparam name="TAsync"></typeparam>
    public interface IConsoleBotConnector<out TSync, out TAsync>
    {
        /// <summary>
        /// Obtains the synchronous communication implementation for this console.
        /// </summary>
        TSync CreateSync();

        /// <summary>
        /// Obtains the asynchronous communication implementation for this console.
        /// </summary>
        TAsync CreateAsynchronous();
    }

    /// <summary>
    /// Combined interface for less verbosity of implementers
    /// </summary>
    public interface IConsoleBotManaged<out TSync, out TAsync> : IConsoleBotConfig, IConsoleBotConnector<TSync, TAsync>
    {
        IConsoleBotConfig GetInnerConfig();
    }
}
