using System;

namespace SysBot.Pokemon.Discord;

public static class SysCordSettings
{
    public static DiscordManager Manager { get; internal set; } = null!;
    public static DiscordSettings Settings => Manager.Config;
    public static PokeTradeHubConfig HubConfig { get; internal set; } = null!;

    public static int CommandsRegistered { get; private set; }
    public static DateTime CommandsRegisteredTime { get; private set; }

    public static void SetCommandsRegistered(int count)
    {
        CommandsRegistered = count;
        CommandsRegisteredTime = DateTime.UtcNow;
    }
}
