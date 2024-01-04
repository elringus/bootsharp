namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of an interop method argument.
/// </summary>
public record ArgumentMeta
{
    /// <summary>
    /// C# name of the argument, as specified in source code.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// JavaScript name of the argument, to be specified in source code.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Type metadata of the argument.
    /// </summary>
    public required TypeMeta Type { get; init; }

    public override string ToString () => $"{Name}: {Type.JSSyntax}";
}
