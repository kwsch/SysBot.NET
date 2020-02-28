using System.Linq;
using System.Text;

namespace SysBot.Pokemon
{
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
    }
}
