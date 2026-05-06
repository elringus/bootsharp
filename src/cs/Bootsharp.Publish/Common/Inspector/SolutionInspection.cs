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
    /// Individual interop members, ie methods or events with <see cref="ExportAttribute"/>
    /// or <see cref="ImportAttribute"/> found on user-defined static classes.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> Static { get; init; }
    /// <summary>
    /// Interop API surfaces specified under assembly-level <see cref="ExportAttribute"/>
    /// or <see cref="ImportAttribute"/> attributes.
    /// </summary>
    public required IReadOnlyCollection<InstancedMeta> Modules { get; init; }
    /// <summary>
    /// All the immutable types that are serialized and copied by value when crossing the interop boundary.
    /// </summary>
    public required IReadOnlyCollection<SerializedMeta> Serialized { get; init; }
    /// <summary>
    /// All the mutable types whose instances are passed by reference when crossing the interop boundary.
    /// </summary>
    public required IReadOnlyCollection<InstancedMeta> Instanced { get; init; }
    /// <summary>
    /// C# XML documentation for the inspected assemblies.
    /// </summary>
    public required IReadOnlyCollection<DocumentationMeta> Documentation { get; init; }
    /// <summary>
    /// Warnings logged while inspecting the solution.
    /// </summary>
    public required IReadOnlyCollection<string> Warnings { get; init; }

    public void Dispose () => ctx.Dispose();
}
