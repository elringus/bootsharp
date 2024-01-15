using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal class AssemblyInspection (MetadataLoadContext ctx) : IDisposable
{
    public ImmutableArray<AssemblyMeta> Assemblies { get; init; } = [];
    public ImmutableArray<InterfaceMeta> Interfaces { get; init; } = [];
    public ImmutableArray<MethodMeta> Methods { get; init; } = [];
    public ImmutableArray<Type> Crawled { get; init; } = [];
    public ImmutableArray<string> Warnings { get; init; } = [];

    public void Dispose () => ctx.Dispose();
}
