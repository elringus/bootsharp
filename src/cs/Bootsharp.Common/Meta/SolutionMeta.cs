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
    /// Interop methods in the solution: either top-level (eg, <see cref="JSInvokableAttribute"/>) or
    /// members of the auto-generated interop classes (eg, <see cref="JSExportAttribute"/>).
    /// </summary>
    public required IReadOnlyCollection<MethodMeta> Methods { get; init; }
    /// <summary>
    /// Types referenced in the interop methods signatures, including
    /// types associated with the prior types, crawled recursively.
    /// </summary>
    public required IReadOnlyCollection<Type> Crawled { get; init; }
    /// <summary>
    /// Interface types specified in <see cref="JSExportAttribute"/>.
    /// </summary>
    public required IReadOnlyCollection<Type> Exports { get; init; }
    /// <summary>
    /// Interface types specified in <see cref="JSImportAttribute"/>.
    /// </summary>
    public required IReadOnlyCollection<Type> Imports { get; init; }
}
