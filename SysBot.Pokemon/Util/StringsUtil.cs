using System;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon;

public static class StringsUtil
{
    /// <summary>
    /// Remove all non-alphanumeric characters, convert wide chars to narrow, and converts the final string to lowercase.
    /// </summary>
    /// <param name="input">User enter-able string</param>
    /// <remarks>
    /// Due to different languages having a different character input keyboard, we may encounter full-width characters.
    /// Strip things down to a-z,0-9 so that we can permissibly compare these user input strings to our magic strings.
    /// </remarks>
    public static string Sanitize(string input)
    {
        var normalize = input.Normalize(NormalizationForm.FormKC);
        var sanitized = normalize.Where(char.IsLetterOrDigit);
        return string.Concat(sanitized.Select(char.ToLower));
    }

    private static readonly char[] Blacklist = ['.', '\\', '/', ',', '*', ';', '．', '・', '。'];
    private static readonly string[] TLD = ["tv", "gg", "yt"];
    private static readonly string[] TLD2 = ["com", "org", "net"];

    /// <summary>
    /// Checks the input <see cref="text"/> to see if it is selfish spam.
    /// </summary>
    /// <param name="text">String to check</param>
    /// <returns>True if spam, false if natural.</returns>
    public static bool IsSpammyString(string text)
    {
        if (text.IndexOfAny(Blacklist) >= 0)
            return true;

        if (text.Length <= 6)
            return false;

        text = text.Replace(" ", "");
        if (text.Contains("pkm", StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (TLD.Any(z => text.EndsWith(z, StringComparison.InvariantCultureIgnoreCase)))
            return true;
        if (TLD2.Any(z => text.EndsWith(z, StringComparison.InvariantCultureIgnoreCase)))
            return true;
        if (TLD.Any(z => text.StartsWith(z, StringComparison.InvariantCultureIgnoreCase)))
            return true;
        return false;
    }
}
