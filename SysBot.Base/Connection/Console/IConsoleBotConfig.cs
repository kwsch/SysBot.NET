namespace SysBot.Base
{
    public interface IConsoleBotConfig
    {
        bool IsValid();
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
