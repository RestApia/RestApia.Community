using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
namespace RestApia.Shared.Common;

public static class StringExtension
{
    public static bool IsEmpty([NotNullWhen(false)] this string? str) => string.IsNullOrWhiteSpace(str);
    public static bool IsNotEmpty([NotNullWhen(true)] this string? str) => !str.IsEmpty();

    public static string TrimStart(this string src, string trim)
    {
        if (string.IsNullOrEmpty(src)) return string.Empty;
        if (string.IsNullOrEmpty(trim)) return src;

        return src.StartsWith(trim) ? src[trim.Length..] : src;
    }

    public static string JoinString(this IEnumerable<object> collection, string separator = ", ", bool includeSpaces = false) =>
        string.Join(separator, collection.Where(x => includeSpaces || !string.IsNullOrWhiteSpace(x?.ToString())));

    public static string TrimEnd(this string src, string trim)
    {
        if (string.IsNullOrEmpty(src)) return string.Empty;
        if (string.IsNullOrEmpty(trim)) return src;

        return src.EndsWith(trim) ? src[..^trim.Length] : src;
    }

    public static IReadOnlyCollection<string> SplitRegex(this string? src, string regex) => string.IsNullOrWhiteSpace(src)
        ? [src ?? string.Empty]
        : Regex.Split(src, regex);

    public static string ToCapitalCase(this string src)
    {
        if (string.IsNullOrWhiteSpace(src)) return string.Empty;
        if (src.Length == 1) return src.ToUpper(CultureInfo.InvariantCulture);
        return char.ToUpper(src[0], CultureInfo.InvariantCulture) + src[1..];
    }

    /// <summary>
    /// Return argument value if source string is empty.
    /// </summary>
    public static string IfEmpty(this string source, string result) => string.IsNullOrWhiteSpace(source)
        ? result
        : source;

    public static string ReplaceRegex(this string input, string pattern, string replacement, RegexOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        if (string.IsNullOrWhiteSpace(pattern)) return input;

        return Regex.Replace(input, pattern, replacement, options ?? RegexOptions.None);
    }

    public static int IndexOfAny(this string input, params string[] searchStrings)
    {
        foreach (var searchString in searchStrings)
        {
            var index = input.IndexOf(searchString, StringComparison.Ordinal);
            if (index != -1)
                return index;
        }

        return -1;
    }
}
