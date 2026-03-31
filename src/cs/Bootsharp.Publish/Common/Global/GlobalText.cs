global using static Bootsharp.Publish.GlobalText;

namespace Bootsharp.Publish;

internal static class GlobalText
{
    public static string JoinLines (IEnumerable<string?> values, int indent = 1, string separator = "\n")
    {
        var pad = new string(' ', indent * 4);
        var padded = values.Where(v => v != null).Select(v => v!.Replace("\n", "\n" + pad));
        return string.Join(separator + pad, padded);
    }

    public static string JoinLines (params string?[] values) => JoinLines(values, 1);
    public static string JoinLines (int indent, params string?[] values) => JoinLines(values, indent);

    public static string ToFirstLower (string value)
    {
        if (value.Length == 1) return value.ToLowerInvariant();
        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
