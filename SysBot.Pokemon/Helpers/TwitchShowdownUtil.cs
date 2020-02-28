using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class TwitchShowdownUtil
    {
        /// <summary>
        /// Converts a single line to a showdown set
        /// </summary>
        /// <param name="setstring">single string</param>
        /// <returns>ShowdownSet object</returns>
        public static ShowdownSet ConvertToShowdown(string setstring)
        {
            // Twitch removes new lines, so we are left with a single line set
            var restorenick = string.Empty;

            var nickIndex = setstring.LastIndexOf(')');
            if (nickIndex > -1)
            {
                restorenick = setstring.Substring(0, nickIndex + 1);
                setstring = setstring.Substring(nickIndex + 1);
            }

            foreach (string i in splittables)
            {
                if (setstring.Contains(i))
                    setstring = setstring.Replace(i, $"\r\n{i}");
            }

            var finalset = restorenick + setstring;
            return new ShowdownSet(finalset);
        }

        private static readonly string[] splittables =
        {
            "Ability:", "EVs:", "IVs:", "Shiny:", "- ", "Level:",
            "Adamant", "Bashful", "Brave", "Bold", "Calm",
            "Careful", "Docile", "Gentle", "Hardy", "Hasty",
            "Impish", "Jolly", "Lax", "Lonely", "Mild",
            "Modest", "Naive", "Naughty", "Quiet", "Quirky",
            "Rash", "Relaxed", "Sassy", "Serious", "Timid"
        };
    }
}
