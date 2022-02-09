using System.Text.RegularExpressions;

namespace Packer;

internal class NamespaceBuilder
{
    private const string separator = "=>";
    private readonly string pattern, replace;

    public NamespaceBuilder (string replacePattern = null)
    {
        if (!string.IsNullOrEmpty(replacePattern))
            (pattern, replace) = ParsePattern(replacePattern);
    }

    public string Build (string assemblyName)
    {
        if (pattern is null) return assemblyName;
        return Regex.Replace(assemblyName, pattern, replace);
    }

    private static (string pattern, string replace) ParsePattern (string replacePattern)
    {
        if (!replacePattern.Contains(separator))
            throw new PackerException($"Invalid namespace pattern: missing '{separator}'.");
        var parts = replacePattern.Split(separator);
        return (parts[0], parts[1]);
    }
}
