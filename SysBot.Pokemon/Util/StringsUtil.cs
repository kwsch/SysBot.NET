using System;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon;

public static class StringsUtil
{
    private static readonly System.Buffers.SearchValues<char> adBadList = System.Buffers.SearchValues.Create(".\\/,*;．・。");

    private static readonly string[] TLD = ["tv", "gg", "yt"];
    private static readonly string[] TLD2 = ["com", "org", "net"];

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

    /// <summary>
    /// Lesser sanitization to only include whitespace lowercase characters.
    /// </summary>
    private static int Sanitize(ReadOnlySpan<char> input, Span<char> output)
    {
        int ctr = 0;
        foreach (var c in input)
        {
            if (char.IsWhiteSpace(c))
                continue;
            output[ctr++] = char.ToLowerInvariant(c);
        }
        return ctr;
    }

    /// <summary>
    /// Checks the input <see cref="text"/> to see if it is selfish spam.
    /// </summary>
    /// <param name="text">String to check</param>
    /// <returns>True if spam, false if natural.</returns>
    public static bool IsSpammyString(ReadOnlySpan<char> text)
    {
        if (text.IndexOfAny(adBadList) >= 0)
            return true;

        if (text.Length <= 6)
            return false;

        Span<char> despaced = stackalloc char[text.Length];
        int len = Sanitize(text, despaced);
        return IsSpammyValue(despaced[..len]);
    }

    private static bool IsSpammyValue(ReadOnlySpan<char> text)
    {
        const StringComparison mode = StringComparison.Ordinal;

        if (text.Contains("pkm", mode))
            return true;

        foreach (var tld in TLD)
        {
            if (text.StartsWith(tld, mode))
                return true;
            if (text.EndsWith(tld, mode))
                return true;
        }
        foreach (var tld in TLD2)
        {
            if (text.EndsWith(tld, mode))
                return true;
        }
        return false;
    }
}
