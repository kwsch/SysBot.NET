using PKHeX.Core;
using System.Text.RegularExpressions;

namespace SysBot.Pokemon
{
    public static class ShowdownUtil
    {
        /// <summary>
        /// Converts a single line to a showdown set
        /// </summary>
        /// <param name="setstring">single string</param>
        /// <returns>ShowdownSet object</returns>
        public static ShowdownSet? ConvertToShowdown(string setstring, out string pokerus, bool eggsallowed = false)
        {
            // LiveStreams remove new lines, so we are left with a single line set
            var restorenick = string.Empty;
            bool eggy = false;
            pokerus = "No";

            if (setstring.Substring(0,3) == "Egg" || setstring.Contains("(Egg)"))
                eggy = true;

            if (setstring.Contains(" Pokerus:"))
            {
                if (setstring.Contains(" Pokerus: Yes") || setstring.Contains(" Pokerus:Yes"))
                {
                    pokerus = "Yes";
                    setstring = setstring.Replace(" Pokerus: Yes", "");
                    setstring = setstring.Replace(" Pokerus:Yes", "");
                }
                else if (setstring.Contains(" Pokerus: No") || setstring.Contains(" Pokerus:No"))
                {
                    setstring = setstring.Replace(" Pokerus: No", "");
                    setstring = setstring.Replace(" Pokerus:No", "");
                }
                else if (setstring.Contains(" Pokerus: Cured") || setstring.Contains(" Pokerus:Cured"))
                {
                    pokerus = "Cured";
                    setstring = setstring.Replace(" Pokerus: Cured", "");
                    setstring = setstring.Replace(" Pokerus:Cured", "");
                }
            }


            if (eggy && eggsallowed) 
            {
                if (!setstring.Contains("Level:")) { 
                    if (setstring.Contains("- "))
                    {
                        var regex = new Regex(Regex.Escape("- "));
                        setstring = regex.Replace(setstring, "Level: 1 - ", 1);
                    }
                    else
                        setstring = setstring + " Level: 1";
                }
                int count = 2;
                while (count < 100)
                {
                    if (setstring.Contains($"Level: {count.ToString()}"))
                        setstring = setstring.Replace($"Level: {count.ToString()}", $"Level: 1");
                    if (setstring.Contains($"Level:{count.ToString()}"))
                        setstring = setstring.Replace($"Level: {count.ToString()}", $"Level: 1");
                    count = count + 1;
                }
            }
            var nickIndex = setstring.LastIndexOf(')');
            if (nickIndex > -1)
            {
                restorenick = setstring[..(nickIndex + 1)];
                if (restorenick.TrimStart().StartsWith("("))
                    return null;
                setstring = setstring[(nickIndex + 1)..];
            }

            foreach (string i in splittables)
            {
                if (setstring.Contains(i))
                    setstring = setstring.Replace(i, $"\r\n{i}");
            }
            var finalset = restorenick + setstring;
            System.IO.File.WriteAllText("FinalSet.txt", finalset);
            return new ShowdownSet(finalset);
        }

        private static readonly string[] splittables =
        {
            "Ability:", "EVs:", "IVs:", "Shiny:", "Gigantamax:", "Ball:", "- ", "Level:",
            "Happiness:", "Language:", "OT:", "OTGender:", "TID:", "SID:", "Alpha:",
            "Adamant Nature", "Bashful Nature", "Brave Nature", "Bold Nature", "Calm Nature",
            "Careful Nature", "Docile Nature", "Gentle Nature", "Hardy Nature", "Hasty Nature",
            "Impish Nature", "Jolly Nature", "Lax Nature", "Lonely Nature", "Mild Nature",
            "Modest Nature", "Naive Nature", "Naughty Nature", "Quiet Nature", "Quirky Nature",
            "Rash Nature", "Relaxed Nature", "Sassy Nature", "Serious Nature", "Timid Nature",
        };
    }
}
