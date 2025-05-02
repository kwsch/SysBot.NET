using System.ComponentModel;

namespace SysBot.Pokemon;

public class FossilSettings
{
    private const string Counts = nameof(Counts);

    private const string Fossil = nameof(Fossil);

    /// <summary>
    /// Toggle for injecting fossil pieces.
    /// </summary>
    [Category(Fossil), Description("Toggle for injecting fossil pieces.")]
    public bool InjectWhenEmpty { get; set; }

    [Category(Fossil), Description("Species of fossil Pokémon to hunt for.")]
    public FossilSpecies Species { get; set; } = FossilSpecies.Dracozolt;

    public override string ToString() => "Fossil Bot Settings";
}
