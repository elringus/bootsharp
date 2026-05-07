namespace Bootsharp.Publish;

/// <summary>
/// Describes a mutable CLR type whose instances are passed by reference when crossing the interop boundary.
/// </summary>
internal record InstancedMeta (Type Clr) : TypeMeta(Clr)
{
    /// <summary>
    /// Whether the type's instances are exported from C# or imported from JavaScript.
    /// </summary>
    public required InteropKind Interop { get; init; }
    /// <summary>
    /// Namespace of the generated C# bindings wrapper.
    /// </summary>
    public required string Namespace { get; init; }
    /// <summary>
    /// Name of the generated C# bindings wrapper.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Full type name of the generated C# bindings wrapper.
    /// </summary>
    public string FullName => $"{Namespace}.{Name}";
    /// <summary>
    /// Name of the generated JavaScript bindings wrapper.
    /// </summary>
    public required string JSName { get; init; }
    /// <summary>
    /// Members declared on the instance.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> Members { get; init; }
    /// <summary>
    /// Name of the specialized C# exporter method or null when <see cref="Instances.Export"/> is sufficient.
    /// </summary>
    public string? Exporter { get; init; }
    /// <summary>
    /// Name of the specialized JS importer function or null when <see cref="Instances.Import"/> is sufficient.
    /// </summary>
    public string? Importer { get; init; }
}
