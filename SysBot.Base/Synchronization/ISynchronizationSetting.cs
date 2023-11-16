namespace SysBot.Base;

public interface ISynchronizationSetting
{
    BotSyncOption SynchronizeBots { get; set; }
    int SynchronizeDelayBarrier { get; set; }
    double SynchronizeTimeout { get; set; }
}
