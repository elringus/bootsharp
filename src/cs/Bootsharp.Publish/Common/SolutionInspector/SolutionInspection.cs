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
    /// Interop interfaces specified under <see cref="ImportAttribute"/> or
    /// <see cref="ExportAttribute"/> for which static bindings have to be emitted.
    /// </summary>
    public required IReadOnlyCollection<InterfaceMeta> StaticInterfaces { get; init; }
    /// <summary>
    /// Interop interfaces found in interop method arguments or return values.
    /// Such interfaces are considered instanced interop APIs, ie stateful objects with
    /// interop methods and properties. Both members of <see cref="StaticInterfaces"/>
    /// and <see cref="StaticMembers"/> can be sources of the instanced interfaces.
    /// </summary>
    public required IReadOnlyCollection<InterfaceMeta> InstancedInterfaces { get; init; }
    /// <summary>
    /// Static interop members, ie methods or events with <see cref="ExportAttribute"/>
    /// or <see cref="ImportAttribute"/> found on user-defined static classes.
    /// </summary>
    public required IReadOnlyCollection<MemberMeta> StaticMembers { get; init; }
    /// <summary>
    /// All the types that cross the interop boundary or referenced by them.
    /// </summary>
    public required IReadOnlyCollection<TypeMeta> Types { get; init; }
    /// <summary>
    /// All the types that require serialization to cross the interop boundary.
    /// </summary>
    public required IReadOnlyCollection<SerializedMeta> Serialized { get; init; }
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
