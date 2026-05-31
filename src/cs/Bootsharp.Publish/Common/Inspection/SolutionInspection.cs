using System.Runtime.Loader;

namespace Bootsharp.Publish;

/// <summary>
/// Metadata about the built C# solution required to generate interop
/// code and other Bootsharp-specific resources.
/// </summary>
/// <param name="ctx">
/// Collectible context in which the solution's assemblies were loaded and inspected.
/// Shouldn't be unloaded to keep C# reflection APIs usable on the inspected types.
/// Dispose to unload the context and reclaim the associated memory.
/// </param>
internal sealed class SolutionInspection (AssemblyLoadContext ctx) : IDisposable
{
    /// <summary>
    /// The discovered interop artifacts.
    /// </summary>
    public required IReadOnlyCollection<TypeMeta> Types { get; init; }
    /// <summary>
    /// C# XML documentation for the inspected assemblies.
    /// </summary>
    public required IReadOnlyCollection<DocMeta> Docs { get; init; }

    public void Dispose () => ctx.Unload();
}
