namespace SysBot.Pokemon;

/// <summary>
/// Indicates the significance of request data.
/// </summary>
public enum RequestSignificance
{
    /// <summary>
    /// Default significance
    /// </summary>
    None,

    /// <summary>
    /// Above-average significance
    /// </summary>
    Favored,

    /// <summary>
    /// Highest significance (testing purposes)
    /// </summary>
    Owner,
}
