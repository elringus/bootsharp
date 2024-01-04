namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of the compiled solution.
/// </summary>
public interface ISolutionMeta
{
    /// <summary>
    /// Assemblies included in the solution.
    /// </summary>
    IReadOnlyList<AssemblyMeta> Assemblies { get; }
    /// <summary>
    /// Interop methods in the solution: either top-level (eg [JSInvokable]) or
    /// members of the auto-generated interop classes (eg [JSExport]).
    /// </summary>
    IReadOnlyList<MethodMeta> Methods { get; }
    /// <summary>
    /// Types referenced in the interop methods signatures, including
    /// types associated with the prior types, crawled recursively.
    /// </summary>
    IReadOnlyList<Type> Types { get; }
}
