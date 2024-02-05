namespace Bootsharp.Publish;

/// <summary>
/// Interface supplied by user under either <see cref="JSExportAttribute"/>
/// or <see cref="JSImportAttribute"/> representing static interop API, or in
/// an interop method, representing instanced interop API.
/// </summary>
internal sealed record InterfaceMeta
{
    /// <summary>
    /// Whether the interface represents C# API consumed in
    /// JavaScript (export) or vice-versa (import).
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
    /// Methods declared on the interface, representing the interop API.
    /// </summary>
    public required IReadOnlyCollection<MethodMeta> Methods { get; init; }
}
