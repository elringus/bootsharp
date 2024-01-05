namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of a C# assembly included in compiled solution.
/// </summary>
public sealed record AssemblyMeta
{
    /// <summary>
    /// Name of the assembly; equals DLL file name, w/o the extension.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Raw binary content of the assembly.
    /// </summary>
    public required byte[] Bytes { get; init; }
}
