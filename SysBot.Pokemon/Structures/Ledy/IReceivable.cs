namespace SysBot.Pokemon
{
    public interface IReceivable<T>
    {
        T Receive { get; }
    }
}