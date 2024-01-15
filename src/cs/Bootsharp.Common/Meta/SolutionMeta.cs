namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of the compiled solution.
/// </summary>
public sealed record SolutionMeta
{
    /// <summary>
    /// Assemblies included in the solution.
    /// </summary>
    public required IReadOnlyCollection<AssemblyMeta> Assemblies { get; init; }
    /// <summary>
    /// Interop interfaces in the solution: supplied by user under either
    /// <see cref="JSExportAttribute"/> or <see cref="JSImportAttribute"/>.
    /// </summary>
    public required IReadOnlyCollection<InterfaceMeta> Interfaces { get; init; }
    /// <summary>
    /// Interop methods in the solution: either top-level (eg, <see cref="JSInvokableAttribute"/>) or
    /// members of the interop classes generated for <see cref="Interfaces"/>.
    /// </summary>
    public required IReadOnlyCollection<MethodMeta> Methods { get; init; }
    /// <summary>
    /// Types referenced in the interop methods signatures, including
    /// types associated with the prior types, crawled recursively.
    /// </summary>
    public required IReadOnlyCollection<Type> Crawled { get; init; }
}
