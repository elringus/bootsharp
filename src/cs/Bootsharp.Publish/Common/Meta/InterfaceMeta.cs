namespace Bootsharp.Publish;

/// <summary>
/// C# interface supplied by user under either <see cref="JSExportAttribute"/>
/// or <see cref="JSImportAttribute"/>, or as a value in an interop method
/// representing an instanced API.
/// </summary>
internal sealed record InterfaceMeta
{
    /// <summary>
    /// Kind of the interface.
    /// </summary>
    public required InterfaceKind Kind { get; init; }
    /// <summary>
    /// C# type of the interface.
    /// </summary>
    public required Type Type { get; init; }
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
    /// Interop methods declared on the interface.
    /// </summary>
    public required IReadOnlyCollection<MethodMeta> Methods { get; init; }
}
