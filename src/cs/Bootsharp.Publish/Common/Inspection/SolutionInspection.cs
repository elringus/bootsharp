using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// Metadata about the built C# solution required to generate interop
/// code and other Bootsharp-specific resources.
/// </summary>
/// <param name="ctx">
/// Context in which the solution's assemblies were loaded and inspected.
/// Shouldn't be disposed to keep C# reflection APIs usable on the inspected types.
/// Dispose to remove file lock on the inspected assemblies.
/// </param>
internal sealed class SolutionInspection (MetadataLoadContext ctx) : IDisposable
{
    /// <summary>
    /// The discovered interop artifacts.
    /// </summary>
    public required IReadOnlyCollection<TypeMeta> Types { get; init; }
    /// <summary>
    /// C# XML documentation for the inspected assemblies.
    /// </summary>
    public required IReadOnlyCollection<DocMeta> Docs { get; init; }
    /// <summary>
    /// Warnings logged while inspecting the solution.
    /// </summary>
    public required IReadOnlyCollection<string> Warnings { get; init; }

    public void Dispose () => ctx.Dispose();
}
