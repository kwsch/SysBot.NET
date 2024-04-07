using System.ComponentModel;
using System.IO;

namespace SysBot.Pokemon;

public class FolderSettings : IDumper
{
    private const string FeatureToggle = nameof(FeatureToggle);
    private const string Files = nameof(Files);
    public override string ToString() => "Ordner / Dumping-Einstellungen";

    [Category(FeatureToggle), Description("Wenn diese Option aktiviert ist, werden alle empfangenen PKM-Dateien (Handelsergebnisse) in den DumpOrdner übertragen.")]
    public bool Dump { get; set; }

    [Category(Files), Description("Quellordner: aus dem die zu verteilenden PKM-Dateien ausgewählt werden.")]
    public string DistributeFolder { get; set; } = string.Empty;

    [Category(Files), Description("Zielordner: In diesen Ordner werden alle empfangenen PKM-Dateien gespeichert.")]
    public string DumpFolder { get; set; } = string.Empty;

    public void CreateDefaults(string path)
    {
        var dump = Path.Combine(path, "dump");
        Directory.CreateDirectory(dump);
        DumpFolder = dump;
        Dump = true;

        var distribute = Path.Combine(path, "distribute");
        Directory.CreateDirectory(distribute);
        DistributeFolder = distribute;
    }
}
