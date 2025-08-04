using PKHeX.Core;
using System.Collections.Generic;

namespace SysBot.Pokemon.Discord;

public class ProcessedPokemonResult<T> where T : PKM, new()
{
    public T? Pokemon { get; set; }
    public string? Error { get; set; }
    public ShowdownSet? ShowdownSet { get; set; }
    public string? LegalizationHint { get; set; }
    public List<Pictocodes>? LgCode { get; set; }
    public bool IsNonNative { get; set; }
}

public class BatchTradeError
{
    public int TradeNumber { get; set; }
    public string SpeciesName { get; set; } = "Unknown";
    public string ErrorMessage { get; set; } = "Unknown error";
    public string? LegalizationHint { get; set; }
    public string ShowdownSet { get; set; } = "";
}

public class OriginalPokemonValues
{
    public bool Shiny { get; set; }
    public int Ability { get; set; }
    public int Ball { get; set; }
    public int Level { get; set; }
    public int Nature { get; set; }
    public ushort[] Moves { get; set; } = new ushort[4];
}
