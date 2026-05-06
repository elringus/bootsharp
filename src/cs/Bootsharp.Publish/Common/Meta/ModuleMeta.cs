namespace Bootsharp.Publish;

/// <summary>
/// Describes a CLR type specified as interop surface under an assembly-level
/// <see cref="ExportAttribute"/> or <see cref="ImportAttribute"/> attribute.
/// </summary>
internal record ModuleMeta (Type Clr) : TypeMeta(Clr)
{
    /// <summary>
    /// Whether the module is exported from C# or imported from JavaScript.
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
    /// Members declared on the module.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> Members { get; init; }
}
