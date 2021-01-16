namespace SysBot.Base
{
    public interface ISwitchConnectionConfig : IConsoleBotManaged<ISwitchConnectionSync, ISwitchConnectionAsync>
    {
        SwitchProtocol Protocol { get; }
        bool UseCRLF { get; }
    }
}
