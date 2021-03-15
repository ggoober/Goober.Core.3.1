using System;
using System.Text.RegularExpressions;

namespace Goober.Core.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex _rxDoubleSpaces = new Regex("[ ]{2,}", RegexOptions.Compiled);

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string TrimSafety(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Trim();
        }

        public static string ToLowerAndTrimSafety(this string value)
        {
            if (string.IsNullOrEmpty(value) == true)
                return string.Empty;

            return value.ToLower().Trim();
        }

        public static string RemoveDoubleSpacesSafety(this string value)
        {
            if (value == null)
                return null;

            return _rxDoubleSpaces.Replace(value, " ");
        }

        public static string SubstringSafety(this string str, int length)
        {
            return str?.Substring(0, Math.Min(length, str.Length));
        }
    }
}
