using PKHeX.Core;
using System.Collections.Generic;
using System.ComponentModel;

namespace SysBot.Pokemon;

public class LegalitySettings
{
    private string DefaultTrainerName = "FurbySysBot";
    private const string Generate = nameof(Generate);
    private const string Misc = nameof(Misc);
    public override string ToString() => "Legalitätsüberprüfungs-Einstellungen";

    // Generate
    [Category(Generate), Description("MGDB-Verzeichnispfad für Wunderkarten.")]
    public string MGDBPath { get; set; } = string.Empty;

    [Category(Generate), Description("Ordner für PKM-Dateien mit Trainerdaten, die für neu generierte PKM-Dateien verwendet werden sollen.")]
    public string GeneratePathTrainerInfo { get; set; } = string.Empty;

    [Category(Generate), Description("Standard-Original-Trainer-Name für PKM-Dateien, die mit keiner der bereitgestellten PKM-Dateien übereinstimmen.")]
    public string GenerateOT
    {
        get => DefaultTrainerName;
        set
        {
            if (!StringsUtil.IsSpammyString(value))
                DefaultTrainerName = value;
        }
    }

    [Category(Generate), Description("Voreingestellte 16-Bit-Trainer-ID (TID) für Anfragen, die mit keiner der bereitgestellten Trainerdaten-Dateien übereinstimmen. Dies sollte eine 5-stellige Zahl sein.")]
    public ushort GenerateTID16 { get; set; } = 12345;

    [Category(Generate), Description("Voreingestellte 16-Bit Secret ID (SID) für Anfragen, die mit keiner der bereitgestellten Trainer-Datendateien übereinstimmen. Dies sollte eine 5-stellige Zahl sein.")]
    public ushort GenerateSID16 { get; set; } = 54321;

    [Category(Generate), Description("Standardsprache für PKM-Dateien, die mit keiner der bereitgestellten PKM-Dateien übereinstimmen.")]
    public LanguageID GenerateLanguage { get; set; } = LanguageID.German;

    [Category(Generate), Description("Wenn PrioritizeGame auf \"True\" gesetzt ist, wird PrioritizeGameVersion verwendet, um mit der Suche nach Begegnungen zu beginnen. Wenn \"False\", wird das neueste Spiel als Version verwendet. Es wird empfohlen, dies auf \"True\" zu belassen..")]
    public bool PrioritizeGame { get; set; } = true;

    [Category(Generate), Description("Gibt das erste Spiel an, das für die Generierung von Begegnungen verwendet wird, oder das aktuelle Spiel, wenn dieses Feld auf \"Any\" gesetzt ist. Setzen Sie PrioritizeGame auf \"true\", um es zu aktivieren. Es wird empfohlen, diesen Wert auf \"Any\" zu belassen.")]
    public GameVersion PrioritizeGameVersion { get; set; } = GameVersion.Any;

    [Category(Generate), Description("Lege alle möglichen legalen Bänder für jedes generierte Pokémon fest.")]
    public bool SetAllLegalRibbons { get; set; }

    [Category(Generate), Description("Lege einen passenden Ball (basierend auf der Farbe) für jedes generierte Pokémon fest.")]
    public bool SetMatchingBalls { get; set; } = true;

    [Category(Generate), Description("Erzwingt den angegebenen Ball, wenn er legal ist.")]
    public bool ForceSpecifiedBall { get; set; } = true;

    [Category(Generate), Description("Es wird davon ausgegangen, dass Stufe 50-Sets Stufe 100-Wettbewerbssets sind.")]
    public bool ForceLevel100for50 { get; set; }

    [Category(Generate), Description("Erfordert HOME-Tracker beim Tausch von Pokémon, die zwischen den Switch-Spielen gereist sein müssen.")]
    public bool EnableHOMETrackerCheck { get; set; }

    [Category(Generate), Description("Die Reihenfolge, in der die Pokémon-Begegnungstypen versucht werden.")]
    public List<EncounterTypeGroup> PrioritizeEncounters { get; set; } =
    [
        EncounterTypeGroup.Egg, EncounterTypeGroup.Slot,
        EncounterTypeGroup.Static, EncounterTypeGroup.Mystery,
        EncounterTypeGroup.Trade,
    ];

    [Category(Generate), Description("Fügt die Kampfversion für Spiele hinzu, die sie unterstützen (nur SWSH), um Pokémon der letzten Generation in Online-Wettkämpfen einzusetzen.")]
    public bool SetBattleVersion { get; set; }

    [Category(Generate), Description("Der Bot erzeugt ein Spass-Pokémon, wenn er ein illegales Set erhält.")]
    public bool EnableEasterEggs { get; set; }

    [Category(Generate), Description("Benutzer können benutzerdefinierte OT, TID, SID und OT Gender in Showdown-Sets einreichen.")]
    public bool AllowTrainerDataOverride { get; set; }

    [Category(Generate), Description("Benutzer können weitere Anpassungen mit Batch-Editor-Befehlen einreichen.")]
    public bool AllowBatchCommands { get; set; }

    [Category(Generate), Description("Maximale Zeit in Sekunden, die beim Erzeugen eines Satzes vor dem Abbruch vergehen darf. Dies verhindert, dass der Bot bei schwierigen Sets einfriert.")]
    public int Timeout { get; set; } = 15;

    // Misc

    [Category(Misc), Description("Löscht HOME-Tracker für geklonte und vom Benutzer angeforderte PKM-Dateien. Es wird empfohlen, diese Funktion zu deaktivieren, um die Erzeugung ungültiger HOME-Daten zu vermeiden.")]
    public bool ResetHOMETracker { get; set; }
}
