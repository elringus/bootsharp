namespace Bootsharp;

/// <summary>
/// Argument of an inspected interop method.
/// </summary>
public record Argument
{
    /// <summary>
    /// C# name of the argument, as specified in source code.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// C# type of the argument.
    /// </summary>
    public required Type Type { get; init; }
    /// <summary>
    /// C# syntax of the argument's type, as specified in source code.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// Whether the argument is optional/nullable.
    /// </summary>
    public required bool Nullable { get; init; }
    /// <summary>
    /// Whether the argument has to be serialized for interop.
    /// </summary>
    public required bool ShouldSerialize { get; init; }
    /// <summary>
    /// JavaScript name of the argument, as will be specified in source code.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// JavaScript (TypeScript) syntax of the argument's type,
    /// as will be specified in source code.
    /// </summary>
    public required string JSTypeSyntax { get; init; }

    public override string ToString () => $"{Name}: {TypeSyntax}";
}
