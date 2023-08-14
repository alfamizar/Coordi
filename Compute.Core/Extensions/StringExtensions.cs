namespace Compute.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return s == null || s.Length == 0;
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
    }
}
