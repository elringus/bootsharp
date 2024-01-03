namespace Bootsharp;

/// <summary>
/// Argument of an inspected method.
/// </summary>
public record Argument
{
    /// <summary>
    /// Name of the argument, as specified in source code.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// C# type of the argument.
    /// </summary>
    public required Type Type { get; init; }
    /// <summary>
    /// Syntax of the argument's type, as specified in source code.
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

    public override string ToString () => $"{Name}: {TypeSyntax}";
}
