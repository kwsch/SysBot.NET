using System.Text;
using System.Text.RegularExpressions;

namespace SysBot.Pokemon
{
    public static class StringsUtil
    {
        public static string Sanitize(string input)
        {
            /* Remove all non-alphanumeric characters, convert wide chars to narrow, and lowercase. */
            var normalized = Regex.Replace(input, "[^a-zA-Z0-9ａ-ｚＡ-Ｚ０-９]", "");
            normalized = normalized.Normalize(NormalizationForm.FormKC).ToLower();
            return normalized;
        }
    }
}
