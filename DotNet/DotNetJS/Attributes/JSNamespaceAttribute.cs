using System;

namespace DotNetJS;

/// <summary>
/// When applied to WASM entry point assembly, overrides namespace
/// generated for the JavaScript bindings and TypeScript declarations.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class JSNamespaceAttribute : Attribute
{
    /// <summary>
    /// Regex pattern to match.
    /// </summary>
    public string Pattern { get; }
    /// <summary>
    /// Replacement for the pattern matches.
    /// </summary>
    public string Replacement { get; }
    /// <summary>
    /// Whether to append type name to the namespace (before replace).
    /// </summary>
    public bool AppendType { get; }

    /// <param name="pattern">Regex pattern to match.</param>
    /// <param name="replacement">Replacement for the pattern matches.</param>
    /// <param name="appendType">Whether to append type name to the namespace (before replace).</param>
    public JSNamespaceAttribute (string pattern, string replacement, bool appendType = false)
    {
        Pattern = pattern;
        Replacement = replacement;
        AppendType = appendType;
    }
}
