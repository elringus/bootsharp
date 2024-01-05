using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal record AssemblyInspection (MetadataLoadContext ctx) : IDisposable
{
    public ImmutableArray<AssemblyMeta> Assemblies { get; init; } = [];
    public ImmutableArray<MethodMeta> Methods { get; init; } = [];
    public ImmutableArray<Type> Types { get; init; } = [];
    public ImmutableArray<string> Warnings { get; init; } = [];

    public void Dispose () => ctx.Dispose();
}
