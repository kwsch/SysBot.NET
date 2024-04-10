using System.ComponentModel;

namespace SysBot.Pokemon;

public class TimingSettings
{
    private const string OpenGame = nameof(OpenGame);
    private const string CloseGame = nameof(CloseGame);
    private const string Raid = nameof(Raid);
    private const string Misc = nameof(Misc);
    public override string ToString() => "Zusätzliche Zeiteinstellungen";

    // Opening the game.
    [Category(OpenGame), Description("Zusätzliche Zeit in Millisekunden, die beim Starten des Spiels auf das Laden der Profile gewartet wird.")]
    public int ExtraTimeLoadProfile { get; set; }

    [Category(OpenGame), Description("Zusätzliche Zeit in Millisekunden, die gewartet wird, um zu prüfen, ob der DLC verwendbar ist.")]
    public int ExtraTimeCheckDLC { get; set; }

    [Category(OpenGame), Description("Zusätzliche Zeit in Millisekunden, die gewartet werden muss, bevor im Titelbildschirm auf A geklickt wird.")]
    public int ExtraTimeLoadGame { get; set; } = 5000;

    [Category(OpenGame), Description("[BDSP] Zusätzliche Zeit in Millisekunden, um nach dem Titelbildschirm auf das Laden der Oberwelt zu warten.")]
    public int ExtraTimeLoadOverworld { get; set; } = 3000;

    // Closing the game.
    [Category(CloseGame), Description("Zusätzliche Zeit in Millisekunden, die nach dem Drücken von HOME gewartet wird, um das Spiel zu minimieren.")]
    public int ExtraTimeReturnHome { get; set; }

    [Category(CloseGame), Description("Zusätzliche Zeit in Millisekunden, die nach dem Klicken zum Schließen des Spiels gewartet wird.")]
    public int ExtraTimeCloseGame { get; set; }

    // Raid-specific timings.
    [Category(Raid), Description("[RaidBot] Zusätzliche Zeit in Millisekunden, um auf das Laden des Raids zu warten, nachdem man auf die Höhle geklickt hat.")]
    public int ExtraTimeLoadRaid { get; set; }

    [Category(Raid), Description("[RaidBot] Zusätzliche Zeit in Millisekunden, um nach dem Klicken auf \"Andere einladen\" zu warten, bevor ein Pokémon gelockt wird.")]
    public int ExtraTimeOpenRaid { get; set; }

    [Category(Raid), Description("[RaidBot] Zusätzliche Zeit in Millisekunden, die gewartet wird, bevor das Spiel geschlossen wird, um den Raid zurückzusetzen.")]
    public int ExtraTimeEndRaid { get; set; }

    [Category(Raid), Description("[RaidBot] Zusätzliche Wartezeit in Millisekunden, nachdem ein Freund akzeptiert wurde.")]
    public int ExtraTimeAddFriend { get; set; }

    [Category(Raid), Description("[RaidBot] Zusätzliche Wartezeit in Millisekunden nach dem Löschen eines Freundes.")]
    public int ExtraTimeDeleteFriend { get; set; }

    // Miscellaneous settings.
    [Category(Misc), Description("[SWSH/SV] Zusätzliche Wartezeit in Millisekunden, nachdem Sie auf + geklickt haben, um eine Verbindung zu Y-Comm herzustellen (SWSH) oder auf L, um eine Online-Verbindung herzustellen (SV).")]
    public int ExtraTimeConnectOnline { get; set; }

    [Category(Misc), Description("Anzahl der Versuche, eine Socket-Verbindung wiederherzustellen, nachdem die Verbindung unterbrochen wurde. Setzen Sie diesen Wert auf -1, um es unendlich oft zu versuchen.")]
    public int ReconnectAttempts { get; set; } = 30;

    [Category(Misc), Description("Zusätzliche Zeit in Millisekunden, die zwischen den Versuchen, die Verbindung wiederherzustellen, gewartet wird. Die Basiszeit beträgt 30 Sekunden.")]
    public int ExtraReconnectDelay { get; set; }

    [Category(Misc), Description("[BDSP] Zusätzliche Zeit in Millisekunden, um auf das Laden der Oberwelt zu warten, nachdem man den Union Room verlassen hat.")]
    public int ExtraTimeLeaveUnionRoom { get; set; } = 1000;

    [Category(Misc), Description("[BDSP] Zusätzliche Zeit in Millisekunden, um zu Beginn jeder Handelsschleife auf das Laden des Y-Menüs zu warten.")]
    public int ExtraTimeOpenYMenu { get; set; } = 500;

    [Category(Misc), Description("[BDSP] Zusätzliche Zeit in Millisekunden, die gewartet wird, bis der Union Room geladen ist, bevor versucht wird, einen Handel einzuleiten.")]
    public int ExtraTimeJoinUnionRoom { get; set; } = 500;

    [Category(Misc), Description("[SV] Zusätzliche Zeit in Millisekunden, um auf das Laden des Poképortals zu warten.")]
    public int ExtraTimeLoadPortal { get; set; } = 1000;

    [Category(Misc), Description("Zusätzliche Zeit in Millisekunden, um auf das Laden der Box zu warten, nachdem ein Handel gefunden wurde.")]
    public int ExtraTimeOpenBox { get; set; } = 1000;

    [Category(Misc), Description("Wartezeit nach dem Öffnen der Tastatur für die Codeeingabe während des Handels.")]
    public int ExtraTimeOpenCodeEntry { get; set; } = 1000;

    [Category(Misc), Description("Wartezeit nach jedem Tastendruck beim Navigieren in den Switch-Menüs oder bei der Eingabe des Link-Codes.")]
    public int KeypressTime { get; set; } = 200;

    [Category(Misc), Description("Aktivieren Sie diese Option, um eingehende Systemaktualisierungen abzulehnen.")]
    public bool AvoidSystemUpdate { get; set; }
}
