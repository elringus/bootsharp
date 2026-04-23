global using static Bootsharp.Publish.GlobalText;

namespace Bootsharp.Publish;

internal static class GlobalText
{
    public static string Fmt (params string?[] txt) => Fmt(txt, 1);
    public static string Fmt (int indent, params string?[] txt) => Fmt(txt, indent);
    public static string Fmt (IEnumerable<string?> txt, int indent = 1, string separator = "\n")
    {
        var pad = new string(' ', indent * 4);
        var padded = txt.Where(v => v != null).Select(v =>
            string.Join("\n", v!.Split('\n').Select((line, i) =>
                i == 0 ? line : string.IsNullOrWhiteSpace(line) ? "" : pad + line)));
        return string.Join(separator + pad, padded);
    }

    public static string ToFirstLower (string value)
    {
        if (value.Length == 1) return value.ToLowerInvariant();
        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
