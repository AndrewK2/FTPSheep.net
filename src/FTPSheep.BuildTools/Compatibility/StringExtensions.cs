#if NET48
using System;

namespace FTPSheep.BuildTools.Compatibility;

/// <summary>
/// Provides string extension methods for .NET Framework 4.8 compatibility
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether a specified substring occurs within this string,
    /// using the specified comparison rules.
    /// </summary>
    public static bool Contains(this string str, string value, StringComparison comparisonType)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return str.IndexOf(value, comparisonType) >= 0;
    }

    /// <summary>
    /// Splits a string into substrings and trims the results.
    /// </summary>
    public static string[] SplitAndTrim(this string str, char separator)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));

        var parts = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Trim();
        }
        return parts;
    }
}
#endif
