namespace SysBot.Base
{
    public interface ISwitchBotConfig : IConsoleBotManaged<ISwitchConnectionSync, ISwitchConnectionAsync>
    {
        SwitchProtocol Protocol { get; set; }
    }
}
