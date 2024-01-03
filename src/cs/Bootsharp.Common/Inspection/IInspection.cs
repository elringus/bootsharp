namespace Bootsharp;

/// <summary>
/// Bootsharp-specific metadata of the compiled solution.
/// </summary>
public interface IInspection
{
    /// <summary>
    /// Inspected solution assemblies.
    /// </summary>
    IReadOnlyList<Assembly> Assemblies { get; }
    /// <summary>
    /// Inspected interop methods: either top-level (eg [JSInvokable]) or
    /// members of the auto-generated interop classes (eg, [JSExport]).
    /// </summary>
    IReadOnlyList<Method> Methods { get; }
    /// <summary>
    /// Types referenced in the interop methods signatures, including
    /// types associated with the prior types, crawled recursively.
    /// </summary>
    IReadOnlyList<Type> Types { get; }
    /// <summary>
    /// Inspection warnings, such as skipped assemblies.
    /// </summary>
    IReadOnlyList<string> Warnings { get; }
}
