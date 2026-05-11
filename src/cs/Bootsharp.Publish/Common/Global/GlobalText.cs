global using static Bootsharp.Publish.GlobalText;
using System.Text;

namespace Bootsharp.Publish;

internal static class GlobalText
{
    public static string Fmt (params string?[] txt) => Fmt(txt, 1);
    public static string Fmt (int indent, params string?[] txt) => Fmt(txt, indent);
    public static string Fmt (IEnumerable<string?> txt, int indent = 1, string separator = "\n")
    {
        var pad = Pad(indent);
        var padded = txt.Where(v => v != null).Select(v =>
            string.Join("\n", v!.Split('\n').Select((line, i) =>
                i == 0 ? line : string.IsNullOrWhiteSpace(line) ? "" : pad + line)));
        return string.Join(separator + pad, padded);
    }

    public static string Pad (int level)
    {
        return new string(' ', level * 4);
    }

    public static string ToFirstLower (string value)
    {
        if (value.Length == 1) return value.ToLowerInvariant();
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    public static string Slugify (string value)
    {
        var bld = new StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
            if (value[i] == '.') bld.Append('/');
            else if (char.IsUpper(value[i]) && i > 0 && char.IsLower(value[i - 1]))
                bld.Append('-').Append(char.ToLowerInvariant(value[i]));
            else bld.Append(char.ToLowerInvariant(value[i]));
        return bld.ToString();
    }
}
