using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspection (MetadataLoadContext context,
    ImmutableArray<Assembly> assemblies, ImmutableArray<Method> methods,
    ImmutableArray<Type> types, ImmutableArray<string> warnings) : IInspection, IDisposable
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
    public IReadOnlyList<Method> Methods { get; } = methods;
    public IReadOnlyList<Type> Types { get; } = types;
    public IReadOnlyList<string> Warnings { get; } = warnings;

    public void Dispose () => context.Dispose();
}
