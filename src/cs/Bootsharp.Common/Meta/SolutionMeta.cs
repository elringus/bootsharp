namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of the compiled solution.
/// </summary>
public sealed record SolutionMeta
{
    /// <summary>
    /// Assemblies included in the solution.
    /// </summary>
    public required IReadOnlyList<AssemblyMeta> Assemblies { get; init; }
    /// <summary>
    /// Interop methods in the solution: either top-level (eg [JSInvokable]) or
    /// members of the auto-generated interop classes (eg [JSExport]).
    /// </summary>
    public required IReadOnlyList<MethodMeta> Methods { get; init; }
    /// <summary>
    /// Types referenced in the interop methods signatures, including
    /// types associated with the prior types, crawled recursively.
    /// </summary>
    public required IReadOnlyList<Type> Types { get; init; }
}
