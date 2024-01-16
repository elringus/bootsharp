using System.Reflection;

namespace Bootsharp.Publish;

internal class AssemblyInspection (MetadataLoadContext ctx) : IDisposable
{
    public IReadOnlyCollection<AssemblyMeta> Assemblies { get; init; } = [];
    public IReadOnlyCollection<InterfaceMeta> Interfaces { get; init; } = [];
    public IReadOnlyCollection<MethodMeta> Methods { get; init; } = [];
    public IReadOnlyCollection<Type> Crawled { get; init; } = [];
    public IReadOnlyCollection<string> Warnings { get; init; } = [];

    public void Dispose () => ctx.Dispose();
}
