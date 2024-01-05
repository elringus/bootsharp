namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of an argument or return type of an interop method.
/// </summary>
public sealed record ValueMeta
{
    /// <summary>
    /// C# type of the value.
    /// </summary>
    public required Type Type { get; init; }
    /// <summary>
    /// C# syntax of the value type, as specified in source code.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// TypeScript syntax of the value type, to be specified in source code.
    /// </summary>
    public required string JSTypeSyntax { get; init; }
    /// <summary>
    /// Whether the value is optional/nullable.
    /// </summary>
    public required bool Nullable { get; init; }
    /// <summary>
    /// Whether the value type is of an async nature (eg, task or promise).
    /// </summary>
    public required bool Async { get; init; }
    /// <summary>
    /// Whether the value is void (when describing method return type).
    /// </summary>
    public required bool Void { get; init; }
    /// <summary>
    /// Whether the value has to be marshalled to/from JSON for interop.
    /// </summary>
    public required bool Serialized { get; init; }
}
