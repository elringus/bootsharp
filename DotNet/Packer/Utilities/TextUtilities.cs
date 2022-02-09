using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Packer;

internal static class TextUtilities
{
    public static IEnumerable<string> SplitLines (string input)
    {
        var line = default(string);
        using var reader = new StringReader(input);
        while ((line = reader.ReadLine()) != null)
            yield return line;
    }

    public static string JoinLines (IEnumerable<string> values, int indent = 1)
    {
        var separator = "\n" + new string(' ', indent * 4);
        return RemoveEmptyLines(string.Join(separator, values));
    }

    public static string JoinLines (params string[] values) => JoinLines(values, 1);

    public static string JoinLines (int indent, params string[] values) => JoinLines(values, indent);

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
