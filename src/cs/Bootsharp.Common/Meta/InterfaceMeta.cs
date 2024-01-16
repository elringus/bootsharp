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
    /// C# syntax of the interface type, as specified in source code.
    /// </summary>
    public required string TypeSyntax { get; init; }
    /// <summary>
    /// Namespace of the generated interop class implementation.
    /// </summary>
    public required string Namespace { get; init; }
    /// <summary>
    /// Name of the generated interop class implementation.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Full type name of the generated interop class implementation.
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";
    /// <summary>
    /// Methods declared on the interface and associated interop counterparts.
    /// </summary>
    public required IReadOnlyCollection<InterfaceMethodMeta> Methods { get; init; }
}
