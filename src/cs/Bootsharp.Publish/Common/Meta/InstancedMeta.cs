namespace Bootsharp.Publish;

/// <summary>
/// Describes a mutable CLR type whose instances are passed by reference when crossing the interop boundary.
/// </summary>
internal sealed record InstancedMeta (Type Clr) : TypeMeta(Clr)
{
    /// <summary>
    /// Whether the type's instances are exported from C# or imported from JavaScript.
    /// </summary>
    public required InteropKind Interop { get; init; }
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
    /// Members declared on the instance.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> Members { get; init; }
}
