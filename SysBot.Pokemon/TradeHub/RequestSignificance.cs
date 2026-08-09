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

public static class RequestSignificanceExtensions
{
    extension(RequestSignificance sig)
    {
        public bool IsFavored => sig is RequestSignificance.Owner or RequestSignificance.Favored;
        public bool IsOwner => sig is RequestSignificance.Owner;
    }
}

