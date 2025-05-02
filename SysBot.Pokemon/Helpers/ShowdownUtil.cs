using PKHeX.Core;

namespace SysBot.Pokemon;

public static class ShowdownUtil
{
    private static readonly string[] splittables =
        [
            "Ability:", "EVs:", "IVs:", "Shiny:", "Gigantamax:", "Ball:", "- ", "Level:",
        "Happiness:", "Language:", "OT:", "OTGender:", "TID:", "SID:", "Alpha:", "Tera Type:",
        "Adamant Nature", "Bashful Nature", "Brave Nature", "Bold Nature", "Calm Nature",
        "Careful Nature", "Docile Nature", "Gentle Nature", "Hardy Nature", "Hasty Nature",
        "Impish Nature", "Jolly Nature", "Lax Nature", "Lonely Nature", "Mild Nature",
        "Modest Nature", "Naive Nature", "Naughty Nature", "Quiet Nature", "Quirky Nature",
        "Rash Nature", "Relaxed Nature", "Sassy Nature", "Serious Nature", "Timid Nature",
    ];

    /// <summary>
    /// Converts a single line to a showdown set
    /// </summary>
    /// <param name="setstring">single string</param>
    /// <returns>ShowdownSet object</returns>
    public static ShowdownSet? ConvertToShowdown(string setstring)
    {
        // LiveStreams remove new lines, so we are left with a single line set
        var restorenick = string.Empty;

        var nickIndex = setstring.LastIndexOf(')');
        if (nickIndex > -1)
        {
            restorenick = setstring[..(nickIndex + 1)];
            if (restorenick.TrimStart().StartsWith('('))
                return null;
            setstring = setstring[(nickIndex + 1)..];
        }

        foreach (string i in splittables)
        {
            if (setstring.Contains(i))
                setstring = setstring.Replace(i, $"\r\n{i}");
        }

        var finalset = restorenick + setstring;
        return new ShowdownSet(finalset);
    }
}
