using System;

namespace Bootsharp;

/// <summary>
/// When applied to WASM entry point assembly, overrides namespace
/// generated for JavaScript bindings and type definitions.
/// </summary>
/// <example>
/// Transform 'Company.Product.Space' into 'Space':
/// <code>[assembly:JSNamespace(@"Company\.Product\.(\S+)", "$1")]</code>
/// </example>
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

    /// <param name="pattern">Regex pattern to match.</param>
    /// <param name="replacement">Replacement for the pattern matches.</param>
    public JSNamespaceAttribute (string pattern, string replacement)
    {
        Pattern = pattern;
        Replacement = replacement;
    }
}
