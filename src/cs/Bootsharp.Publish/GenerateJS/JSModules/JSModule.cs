namespace Bootsharp.Publish;

/// <summary>
/// A JavaScript module projecting an interop surface under a namespace.
/// </summary>
internal sealed record JSModule
{
    /// <summary>
    /// Path to the module file, without file extension.
    /// </summary>
    public string Path { get; }
    /// <summary>
    /// Alias used when importing and referencing the module in other modules.
    /// </summary>
    public string Alias { get; }
    /// <summary>
    /// The graph of the JavaScript nodes declared under the module.
    /// </summary>
    public IReadOnlyCollection<JSNode> Nodes { get; }

    private readonly string mod, gen, root;

    public JSModule (string path, IReadOnlyList<TypeMeta> types)
    {
        Path = path;
        Alias = path.Replace('/', '_').Replace('-', '_');
        Nodes = Graph(types.Select(type => (type, path: type.JSNode)));
        var depth = path.Count(c => c == '/');
        mod = depth == 0 ? "./" : string.Concat(Enumerable.Repeat("../", depth));
        gen = string.Concat(Enumerable.Repeat("../", depth + 1));
        root = string.Concat(Enumerable.Repeat("../", depth + 2));
    }

    /// <summary>
    /// Builds relative path from this module to a "mjs" file with the specified path
    /// stored under the package's root directory.
    /// </summary>
    public string To (string filename) => $"{root}{filename}.mjs";
    /// <summary>
    /// Builds relative path from this module to a "g.mjs" file with the specified path
    /// stored under the package's 'generated' directory.
    /// </summary>
    public string ToGen (string filename) => $"{gen}{filename}.g.mjs";
    /// <summary>
    /// Builds relative path from this module to a "g.mjs" module file with the specified path
    /// stored under the package's 'generated/modules' directory.
    /// </summary>
    public string ToMd (string path) => $"{mod}{path}.g.mjs";

    private static IReadOnlyList<JSNode> Graph (IEnumerable<(TypeMeta type, string path)> types) => types
        .GroupBy(o => o.path.Split('.', 2)[0])
        .Select(g => new JSNode {
            Name = g.Key,
            Types = g.Where(x => x.path == g.Key).Select(x => x.type).ToArray(),
            Children = Graph(g.Where(x => x.path != g.Key).Select(x => (x.type, x.path[(g.Key.Length + 1)..]))).ToArray()
        })
        .ToArray();
}
