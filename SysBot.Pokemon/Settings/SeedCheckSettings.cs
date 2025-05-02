using System.ComponentModel;

namespace SysBot.Pokemon;

public class SeedCheckSettings
{
    private const string FeatureToggle = nameof(FeatureToggle);

    [Category(FeatureToggle), Description("Allows returning only the closest shiny frame, the first star and square shiny frames, or the first three shiny frames.")]
    public SeedCheckResults ResultDisplayMode { get; set; }

    [Category(FeatureToggle), Description("When enabled, seed checks will return all possible seed results instead of the first valid match.")]
    public bool ShowAllZ3Results { get; set; }

    public override string ToString() => "Seed Check Settings";
}

public enum SeedCheckResults
{
    ClosestOnly,            // Only gets the first shiny

    FirstStarAndSquare,     // Gets the first star shiny and first square shiny

    FirstThree,             // Gets the first three frames
}
