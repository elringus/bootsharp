using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bootsharp.Builder;

internal static class TextUtilities
{
    public static string JoinLines (IEnumerable<string?> values, int indent = 1, bool indentFirst = false, string separator = "\n")
    {
        if (indent > 0) separator += new string(' ', indent * 4);
        var result = RemoveEmptyLines(string.Join(separator, values.Where(v => v is not null)));
        return indentFirst ? separator + result : result;
    }

    public static string JoinLines (params string?[] values) => JoinLines(values, 1);
    public static string JoinLines (int indent, params string?[] values) => JoinLines(values, indent);

    public static string ToFirstLower (string value)
    {
        if (value.Length == 1) char.ToLowerInvariant(value[0]);
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static string RemoveEmptyLines (string content)
    {
        return Regex.Replace(content, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).Trim();
    }
}
