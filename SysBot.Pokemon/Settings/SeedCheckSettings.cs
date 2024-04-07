using System.ComponentModel;

namespace SysBot.Pokemon;

public class SeedCheckSettings
{
    private const string FeatureToggle = nameof(FeatureToggle);
    public override string ToString() => "Einstellungen für SeedCheck";

    [Category(FeatureToggle), Description("Wenn diese Option aktiviert ist, werden bei der Prüfung von Seeds alle möglichen Seed-Ergebnisse anstelle der ersten gültigen Übereinstimmung zurückgegeben.")]
    public bool ShowAllZ3Results { get; set; }

    [Category(FeatureToggle), Description("Ermöglicht die Rückgabe nur des nächstgelegenen ShinyFrames, des ersten Sterns und der ersten quadratischen Shiny oder der ersten drei Shinys.")]
    public SeedCheckResults ResultDisplayMode { get; set; }
}

public enum SeedCheckResults
{
    ClosestOnly,            // Only gets the first shiny
    FirstStarAndSquare,     // Gets the first star shiny and first square shiny
    FirstThree,             // Gets the first three frames
}
