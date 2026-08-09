using System;

namespace SysBot.Pokemon.Discord;

public static class SysCordSettings
{
    public static DiscordManager Manager { get; internal set; } = null!;
    public static DiscordSettings Settings => Manager.Config;
    public static PokeTradeHubConfig HubConfig { get; internal set; } = null!;

    public static int RegisteredCommands { get; private set; }
    public static int RegisteredModals { get; private set; }
    public static DateTime RegisteredTime { get; private set; }

    public static void SetCommandsRegistered(int countCommand, int countModal)
    {
        RegisteredCommands = countCommand;
        RegisteredModals = countModal;
        RegisteredTime = DateTime.UtcNow;
    }
}
