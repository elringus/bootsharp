namespace Bootsharp.Publish;

/// <summary>
/// Describes a CLR type that projects an interop API surface.
/// </summary>
internal abstract record SurfaceMeta (Type Clr) : TypeMeta(Clr)
{
    /// <summary>
    /// Interop API members declared on the surface.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> Members { get; init; }
}

/// <summary>
/// Describes an interop surface encompassing static interop members specified via class-level
/// <see cref="ExportAttribute"/> or <see cref="ImportAttribute"/> attributes.
/// </summary>
internal record StaticMeta (Type Clr) : SurfaceMeta(Clr);

/// <summary>
/// Describes an interop surface that uses a generated proxy to bind with the source.
/// </summary>
internal abstract record ProxyMeta (Type Clr) : SurfaceMeta(Clr)
{
    /// <summary>
    /// Whether the proxy is exported from C# or imported from JavaScript.
    /// </summary>
    public required InteropKind IK { get; init; }
    /// <summary>
    /// Describes the generated proxy.
    /// </summary>
    public required SurfaceProxy Proxy { get; init; }
}

/// <summary>
/// Describes an interop surface specified via assembly-level <see cref="ExportAttribute"/> or
/// <see cref="ImportAttribute"/> attributes.
/// </summary>
internal record ModuleMeta (Type Clr) : ProxyMeta(Clr);

/// <summary>
/// Describes an interop surface projected from an instanced type.
/// Instanced are mutable types whose instances are passed by reference when crossing the interop boundary.
/// </summary>
/// <remarks>
/// Note that 2 instance surfaces are possible for a single instanced type in cases when the type participates
/// in both exported and imported interop directions; for example, an interface get+set property may get
/// instances implemented in JavaScript and set instances of the same interface, but implemented in C#.
/// </remarks>
internal record InstanceMeta (Type Clr) : ProxyMeta(Clr)
{
    /// <summary>
    /// Name of the specialized C# exporter method or null when not required.
    /// </summary>
    public string? Exporter { get; init; }
    /// <summary>
    /// Name of the specialized JS importer function or null when not required.
    /// </summary>
    public string? Importer { get; init; }
}

/// <summary>
/// Describes the generated proxy used by <see cref="ProxyMeta"/>.
/// </summary>
public record SurfaceProxy
{
    /// <summary>
    /// Unique identifier of the generated C# proxy type.
    /// </summary>
    public required string Id { get; init; }
    /// <summary>
    /// Namespace of the generated C# proxy type.
    /// </summary>
    public required string Space { get; init; }
    /// <summary>
    /// Type name of the generated C# proxy type.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// Fully qualified C# syntax of the generated C# proxy type.
    /// </summary>
    public required string Syntax { get; init; }
    /// <summary>
    /// Full object name of the generated proxy on the JavaScript side.
    /// </summary>
    public required string JS { get; init; }
}
