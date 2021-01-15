namespace SysBot.Base
{
    public interface IConsoleBotConfig
    {
        bool IsValid();
        bool Matches(string magic);
    }

    public interface IConsoleBotConnector<out TSync, out TAsync>
    {
        TSync CreateSync();
        TAsync CreateAsynchronous();
    }

    public interface IConsoleBotManaged<out TSync, out TAsync> : IConsoleBotConfig, IConsoleBotConnector<TSync, TAsync>
    {
    }
}
