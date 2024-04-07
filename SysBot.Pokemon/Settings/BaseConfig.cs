using System.ComponentModel;

namespace SysBot.Pokemon;

/// <summary>
/// Console agnostic settings
/// </summary>
public abstract class BaseConfig
{
    protected const string FeatureToggle = nameof(FeatureToggle);
    protected const string Operation = nameof(Operation);
    private const string Debug = nameof(Debug);

    [Category(FeatureToggle), Description("Wenn diese Funktion aktiviert ist, drückt der Bot gelegentlich die B-Taste, wenn er gerade nichts verarbeitet (um den Schlaf zu vermeiden).")]
    public bool AntiIdle { get; set; }

    [Category(FeatureToggle), Description("Aktiviert Textprotokolle. Starten Sie neu, um die Änderungen zu übernehmen.")]
    public bool LoggingEnabled { get; set; } = true;

    [Category(FeatureToggle), Description("Maximale Anzahl der alten Textprotokolldateien, die aufbewahrt werden sollen. Setzen Sie diesen Wert auf <= 0, um die Protokollbereinigung zu deaktivieren. Starten Sie neu, um die Änderungen zu übernehmen.")]
    public int MaxArchiveFiles { get; set; } = 14;

    [Category(Debug), Description("Überspringt das erstellen von Bots wenn die App gestartet wird; hilfreich für das Testen von Integrationen.")]
    public bool SkipConsoleBotCreation { get; set; }

    [Category(Operation)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public LegalitySettings Legality { get; set; } = new();

    [Category(Operation)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public FolderSettings Folder { get; set; } = new();

    public abstract bool Shuffled { get; }
}
