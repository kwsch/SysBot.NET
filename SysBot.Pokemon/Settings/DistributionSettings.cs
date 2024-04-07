using PKHeX.Core;
using SysBot.Base;
using System.ComponentModel;

namespace SysBot.Pokemon;

public class DistributionSettings : ISynchronizationSetting
{
    private const string Distribute = nameof(Distribute);
    private const string Synchronize = nameof(Synchronize);
    public override string ToString() => "Einstellungen für den Handel";

    // Distribute

    [Category(Distribute), Description("Wenn diese Option aktiviert ist, werden inaktive LinkTrade-Bots zufällig PKM-Dateien aus dem DistributeFolder verteilen.")]
    public bool DistributeWhileIdle { get; set; } = true;

    [Category(Distribute), Description("Wenn diese Option aktiviert ist, werden die Verteilungsordner nicht in der gleichen Reihenfolge, sondern nach dem Zufallsprinzip ausgegeben.")]
    public bool Shuffled { get; set; }

    [Category(Distribute), Description("Wenn diese Option auf einen anderen Wert als \"None\" gesetzt wird, ist diese Art zusätzlich zur Übereinstimmung mit dem Spitznamen für den Zufallshandel erforderlich.")]
    public Species LedySpecies { get; set; } = Species.None;

    [Category(Distribute), Description("Wenn diese Option auf \"true\" gesetzt ist, wird der Tausch von zufälligen Ledy-Nicknamen abgebrochen, anstatt eine zufällige Entität aus dem Pool zu tauschen.")]
    public bool LedyQuitIfNoMatch { get; set; }

    [Category(Distribute), Description("Handels Link-Code")]
    public int TradeCode { get; set; } = 1337;

    [Category(Distribute), Description("Trade Link-Code verwendet den Min- und Max-Bereich und nicht den festen Trade Code.")]
    public bool RandomCode { get; set; }

    [Category(Distribute), Description("Bei BDSP begibt sich der Verteilungsbot in einen bestimmten Raum und bleibt dort, bis er angehalten wird.")]
    public bool RemainInUnionRoomBDSP { get; set; } = true;

    // Synchronize

    [Category(Synchronize), Description("Link Trade: Verwendung mehrerer Verteilungsbots - alle Bots bestätigen ihren Handelscode gleichzeitig. Bei lokalem Handel fahren die Bots fort, wenn alle an der Barriere sind. Bei Remote muss den Bots ein anderes Signal zum Fortfahren gegeben werden.")]
    public BotSyncOption SynchronizeBots { get; set; } = BotSyncOption.LocalSync;

    [Category(Synchronize), Description("Link Trade: Verwendung mehrerer Verteilungsbots - sobald alle Bots bereit sind, den Handelscode zu bestätigen, wartet der Hub X Millisekunden, bevor er alle Bots freigibt.")]
    public int SynchronizeDelayBarrier { get; set; }

    [Category(Synchronize), Description("Link Trade: Verwendung mehrerer Verteilungsbots -- wie lange (Sekunden) ein Bot auf die Synchronisierung wartet, bevor er weitermacht.")]
    public double SynchronizeTimeout { get; set; } = 90;
}
