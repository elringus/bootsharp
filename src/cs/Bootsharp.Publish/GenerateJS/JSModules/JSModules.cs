namespace Bootsharp.Publish;

/// <summary>
/// The <see cref="JSModule"/> collection projecting solution interop surface.
/// </summary>
internal sealed class JSModules
{
    /// <summary>
    /// All the JavaScript modules in the solution.
    /// </summary>
    public IReadOnlyCollection<JSModule> List { get; }

    private readonly IReadOnlyCollection<TypeMeta> types;
    private readonly Dictionary<string, JSModule> mdByPath;
    private readonly Dictionary<JSModule, Dictionary<string, JSModule>> importedByMd = [];

    public JSModules (IReadOnlyCollection<TypeMeta> types)
    {
        List = types.GroupBy(t => t.JSModule).Select(g => new JSModule(g.Key, g.ToArray())).ToArray();
        mdByPath = List.ToDictionary(m => m.Path);
        this.types = types;
    }

    /// <summary>
    /// Returns fully qualified node reference to the specified type.
    /// When <paramref name="fromMd"/> is specified, will track the import.
    /// </summary>
    public string Ref (TypeMeta type, JSModule? fromMd = null)
    {
        var toMd = mdByPath[type.JSModule];
        if (fromMd == toMd) return type.JSNode;
        if (fromMd != null) Import(fromMd, toMd);
        return $"{toMd.Alias}.{type.JSNode}";
    }

    /// <inheritdoc cref="Ref(System.Type,Bootsharp.Publish.JSModule?)"/>
    public string Ref (Type clr, JSModule? fromMd = null)
    {
        return Ref(types.Get(clr), fromMd);
    }

    /// <summary>
    /// Returns all modules imported via <see cref="Ref"/> from the specified module.
    /// </summary>
    public IReadOnlyCollection<JSModule> GetImported (JSModule fromMd)
    {
        return importedByMd.TryGetValue(fromMd, out var importedByPath)
            ? importedByPath.Values.ToArray() : [];
    }

    private void Import (JSModule from, JSModule to)
    {
        if (!importedByMd.TryGetValue(from, out var importedByPath))
            importedByMd[from] = importedByPath = [];
        importedByPath[to.Path] = to;
    }
}
