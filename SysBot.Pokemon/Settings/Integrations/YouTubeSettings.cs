using System;
using System.ComponentModel;
using System.Linq;

namespace SysBot.Pokemon;

public class YouTubeSettings
{
    private const string Startup = nameof(Startup);
    private const string Operation = nameof(Operation);
    private const string Messages = nameof(Messages);
    public override string ToString() => "YouTube Integration Settings";

    // Startup

    [Category(Startup), Description("Bot ClientID")]
    public string ClientID { get; set; } = string.Empty;

    [Category(Startup), Description("Bot Client Secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [Category(Startup), Description("ChannelID zum Senden von Nachrichten an")]
    public string ChannelID { get; set; } = string.Empty;

    [Category(Startup), Description("Bot-Befehls-Präfix")]
    public char CommandPrefix { get; set; } = '$';

    [Category(Operation), Description("Nachricht, die gesendet wird, wenn die Schranke aufgehoben wird.")]
    public string MessageStart { get; set; } = string.Empty;

    // Operation

    [Category(Operation), Description("Sudo Usernames")]
    public string SudoList { get; set; } = string.Empty;

    [Category(Operation), Description("Benutzer mit diesen Benutzernamen können den Bot nicht verwenden.")]
    public string UserBlacklist { get; set; } = string.Empty;

    public bool IsSudo(string username)
    {
        var sudos = SudoList.Split([ ",", ", ", " " ], StringSplitOptions.RemoveEmptyEntries);
        return sudos.Contains(username);
    }
}

public enum YouTubeMessageDestination
{
    Disabled,
    Channel,
}
