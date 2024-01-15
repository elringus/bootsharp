namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of a C# interface supplied by user
/// under either <see cref="JSExportAttribute"/> or <see cref="JSImportAttribute"/>.
/// </summary>
public sealed record InterfaceMeta
{
    /// <summary>
    /// Kind of the interface.
    /// </summary>
    public required InterfaceKind Kind { get; init; }
    /// <summary>
    /// Associated C# type of the interface.
    /// </summary>
    public required Type Type { get; init; }
}
