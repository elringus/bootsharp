namespace Bootsharp;

/// <summary>
/// When applied to WASM entry point assembly, configures Bootsharp behaviour at build time.
/// </summary>
/// <remarks>
/// Each attribute property expects array of pattern and replacement string pairs, which are
/// supplied to Regex.Replace when generating associated JavaScript code. Each consequent pair
/// is tested in order; on first match the result replaces the default.
/// </remarks>
/// <example>
/// Make all spaces starting with "Foo.Bar" replaced with "Baz":
/// <code>
/// [assembly: Bootsharp.Preferences(
///     Space = ["^Foo\.Bar\.(\S+)", "Baz.$1"]
/// )]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PreferencesAttribute : Attribute
{
    /// <summary>
    /// Customize how C# type namespaces transform into JavaScript module names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against the C# type namespace, or 'index' when the type is global.
    /// </remarks>
    public string[] Space { get; init; } = [];
    /// <summary>
    /// Customize how C# type names transform into JavaScript object names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against the C# type names, with generic identity removed.
    /// </remarks>
    public string[] Name { get; init; } = [];
    /// <summary>
    /// Customize how C# method names transform into JavaScript function names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against the C# reflected method names.
    /// </remarks>
    public string[] Method { get; init; } = [];
    /// <summary>
    /// Customize how C# property names transform into JavaScript property names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against the C# reflected property names.
    /// </remarks>
    public string[] Property { get; init; } = [];
    /// <summary>
    /// Customize how C# event names transform into JavaScript event names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against the C# reflected event names.
    /// </remarks>
    public string[] Event { get; init; } = [];
}
