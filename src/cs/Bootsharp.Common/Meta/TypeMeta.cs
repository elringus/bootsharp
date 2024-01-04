namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of a type referenced in interop method.
/// </summary>
public record TypeMeta
{
    /// <summary>
    /// Underlying C# type.
    /// </summary>
    public required Type Type { get; init; }
    /// <summary>
    /// C# syntax of the type, as specified in source code.
    /// </summary>
    public required string Syntax { get; init; }
    /// <summary>
    /// TypeScript syntax of the type, to be specified in source code.
    /// </summary>
    public required string JSSyntax { get; init; }
    /// <summary>
    /// Whether the type is optional/nullable.
    /// </summary>
    public required bool Nullable { get; init; }
    /// <summary>
    /// Whether the type is of an async nature (eg, task or promise).
    /// </summary>
    public required bool TaskLike { get; init; }
    /// <summary>
    /// Whether the type is void (when method return type).
    /// </summary>
    public required bool Void { get; init; }
    /// <summary>
    /// Whether the type has to be serialized for interop.
    /// </summary>
    public required bool ShouldSerialize { get; init; }
}
