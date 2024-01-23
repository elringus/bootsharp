using System.Reflection;

namespace Bootsharp.Publish;

internal class AssemblyInspection (MetadataLoadContext ctx) : IDisposable
{
    public required IReadOnlyCollection<InterfaceMeta> Interfaces { get; init; }
    public required IReadOnlyCollection<MethodMeta> Methods { get; init; }
    public required IReadOnlyCollection<Type> Crawled { get; init; }
    public required IReadOnlyCollection<string> Warnings { get; init; }

    public void Dispose () => ctx.Dispose();
}
