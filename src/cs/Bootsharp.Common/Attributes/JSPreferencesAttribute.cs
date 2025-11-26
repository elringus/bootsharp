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
/// [assembly: Bootsharp.JSPreferences(
///     Space = ["^Foo\.Bar\.(\S+)", "Baz.$1"]
/// )]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class JSPreferencesAttribute : Attribute
{
    /// <summary>
    /// Customize generated JavaScript object names and TypeScript namespaces.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against full type name (namespace.typename) of
    /// declaring C# type when generating JavaScript objects for interop methods
    /// and against namespace when generating TypeScript syntax for C# types.
    /// Matched type names have following modifications:<br/>
    ///  - interfaces have first character removed<br/>
    ///  - generics have parameter spec removed<br/>
    ///  - nested have "+" replaced with "."<br/>
    /// </remarks>
    public string[] Space { get; init; } = [];
    /// <summary>
    /// Customize generated TypeScript type syntax.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against full C# type names of
    /// interop method arguments, return values and object properties.
    /// </remarks>
    public string[] Type { get; init; } = [];
    /// <summary>
    /// Customize which C# methods should be transformed into JavaScript
    /// events, as well as generated event names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against C# method names declared under
    /// <see cref="JSImportAttribute"/> interfaces. By default, methods
    /// starting with "Notify.." are matched.
    /// </remarks>
    public string[] Event { get; init; } = [];
    /// <summary>
    /// Customize generated JavaScript function names.
    /// </summary>
    /// <remarks>
    /// The patterns are matched against C# interop method names.
    /// </remarks>
    public string[] Function { get; init; } = [];
}
