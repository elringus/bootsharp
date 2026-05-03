namespace Bootsharp.Publish;

/// <summary>
/// Interface supplied by user under either <see cref="ExportAttribute"/>
/// or <see cref="ImportAttribute"/> representing static interop API, or in
/// an interop method, representing instanced interop API.
/// </summary>
internal sealed record InstancedMeta
{
    /// <summary>
    /// Whether the interface represents C# API consumed in
    /// JavaScript (export) or vice versa (import).
    /// </summary>
    public required InteropKind Interop { get; init; }
    /// <summary>
    /// Type info of the instance.
    /// </summary>
    public required TypeMeta Type { get; init; }
    /// <summary>
    /// C# namespace of the generated interop class implementation.
    /// </summary>
    public required string Namespace { get; init; }
    /// <summary>
    /// C# name of the generated interop class implementation.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Full C# type name of the generated interop class implementation.
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";
    /// <summary>
    /// JS name of the generated interop class implementation.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Members declared on the interface, representing the interop API.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> Members { get; init; }
}
