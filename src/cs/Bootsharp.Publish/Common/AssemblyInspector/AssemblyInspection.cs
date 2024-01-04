using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspection (MetadataLoadContext context,
    ImmutableArray<AssemblyMeta> assemblies, ImmutableArray<MethodMeta> methods,
    ImmutableArray<Type> types, ImmutableArray<string> warnings) : ISolutionMeta, IDisposable
{
    public IReadOnlyList<AssemblyMeta> Assemblies { get; } = assemblies;
    public IReadOnlyList<MethodMeta> Methods { get; } = methods;
    public IReadOnlyList<Type> Types { get; } = types;
    public IReadOnlyList<string> Warnings { get; } = warnings;

    public void Dispose () => context.Dispose();
}
