namespace SysBot.Pokemon
{
    public interface IPoolSettings
    {
        bool DistributeShuffled { get; }
        string DistributeFolder { get; }
        bool ResetHOMETracker { get; }
    }
}