global using static Bootsharp.Publish.TextUtilities;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal static partial class TextUtilities
{
    public static string JoinLines (IEnumerable<string?> values, int indent = 1, string separator = "\n")
    {
        if (indent > 0) separator += new string(' ', indent * 4);
        return RemoveEmptyLines(string.Join(separator, values.Where(v => v is not null)));
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
        return RegexEmptyLines().Replace(content, string.Empty).Trim();
    }

    [GeneratedRegex(@"^\s*$\n|\r", RegexOptions.Multiline)]
    private static partial Regex RegexEmptyLines ();
}
