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

        public static bool IsSet(this string? s)
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

            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
