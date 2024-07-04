using System.Globalization;
using System.Text;

namespace Compute.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return s == null || s.Length == 0;
        }

        public static bool IsNotNullOrEmpty(this string? s)
        {
            return s != null && s.Length != 0;
        }

        public static bool IsSet(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        public static bool IsNotSet(this string s)
        {
            return !s.IsSet();
        }

        public static bool Contains(this string source, string toCheck, StringComparison comparison)
        {
            return source?.IndexOf(toCheck, comparison) >= 0;
        }

        public static string RemoveAccents(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Normalize the input string to FormD (decomposed form)
            var normalizedString = input.Normalize(NormalizationForm.FormD);

            // Use a StringBuilder to collect non-accented characters
            var stringBuilder = new StringBuilder();

            // Iterate through each character in the normalized string
            foreach (var c in normalizedString)
            {
                // Get the Unicode category of the character
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

                // If the character is a non-spacing mark (combining character), skip it
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Normalize back to FormC (composed form) to rebuild the string
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
