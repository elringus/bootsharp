using System.Collections.Immutable;
using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class AssemblyInspection (
    ImmutableArray<Assembly> assemblies, ImmutableArray<Method> methods,
    ImmutableArray<Type> types, ImmutableArray<string> warnings,
    ImmutableArray<MetadataLoadContext> contexts) : IDisposable
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
    public IReadOnlyList<Method> Methods { get; } = methods;
    public IReadOnlyList<Type> Types { get; } = types;
    public IReadOnlyList<string> Warnings { get; } = warnings;

    private readonly IReadOnlyList<MetadataLoadContext> contexts = contexts;

    public void Dispose ()
    {
        foreach (var context in contexts)
            context.Dispose();
    }

    public AssemblyInspection Merge (AssemblyInspection other) => new(
        [..assemblies, ..other.Assemblies],
        [..methods, ..other.Methods],
        [..types, ..other.Types],
        [..warnings, ..other.Warnings],
        [..contexts, ..other.contexts]);
}
