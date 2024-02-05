namespace Bootsharp.Publish;

/// <summary>
/// Interop method argument.
/// </summary>
internal sealed record ArgumentMeta
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
    /// Metadata of the argument's value.
    /// </summary>
    public required ValueMeta Value { get; init; }

    public override string ToString () => $"{Name}: {Value.JSTypeSyntax}";
}
