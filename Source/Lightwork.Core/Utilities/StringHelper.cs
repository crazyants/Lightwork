using System;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace D3.Lightwork.Core.Utilities
{
    public static class StringHelper
    {
        public static string ToTitleCase(this string source)
        {
            var textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(source);
        }

        /// <summary>
        ///     Returns true if <c>str</c> is null, empty or whitespace only (if <c>ignoreWhitespace</c> is true).
        /// </summary>
        [Pure]
        public static bool IsEmpty(this string str, bool ignoreWhitespace = true)
        {
            return ignoreWhitespace ? string.IsNullOrWhiteSpace(str) : string.IsNullOrEmpty(str);
        }

        /// <summary>
        ///     Returns true if <c>str</c> is not null, empty or whitespace only (if <c>ignoreWhitespace</c> is true).
        /// </summary>
        [Pure]
        public static bool IsNotEmpty(this string str, bool ignoreWhitespace = true)
        {
            return !str.IsEmpty(ignoreWhitespace);
        }

        /// <summary>
        ///     Returns <c>str</c> if <c>str</c> is not null, empty or whitespace only (if <c>ignoreWhitespace</c> is true).
        ///     Otherwise <c>defaultValue</c> is returned.
        /// </summary>
        [Pure]
        public static string GetNonEmptyOrDefault(
            this string str,
            string defaultValue = null,
            bool ignoreWhitespace = true)
        {
            return str.IsNotEmpty(ignoreWhitespace) ? str : defaultValue;
        }

        /// <summary>
        ///     Formats string.
        /// </summary>
        [Pure]
        public static string Fmt(this string str, params object[] args)
        {
            str.NotNull();
            args.NotNull();

            return string.Format(str, args);
        }

        /// <summary>
        ///     Formats string.
        /// </summary>
        [Pure]
        public static string Fmt(
            this string str,
            IFormatProvider formatProfider,
            params object[] args)
        {
            str.NotNull();
            args.NotNull();

            return string.Format(formatProfider, str, args);
        }
    }
}
