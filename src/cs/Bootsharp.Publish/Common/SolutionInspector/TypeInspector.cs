namespace Bootsharp.Publish;

internal sealed class TypeInspector
{
    private readonly Dictionary<Type, TypeMeta> byType = [];
    private readonly SerializedInspector serde = new();
    private readonly InstancedInspector itd;
    private InteropKind ik;

    public TypeInspector (Preferences prefs)
    {
        itd = new(new(prefs, Inspect));
    }

    public void Crawl (Type type, InteropKind ik)
    {
        this.ik = ik;
        Crawl(type);
    }

    public IReadOnlyCollection<TypeMeta> Collect ()
    {
        return byType.Values.Distinct().Where(m => IsUserType(m.Clr)).ToArray();
    }

    private void Crawl (Type type)
    {
        if (!byType.TryAdd(type, Inspect(type, ik))) return;
        if (IsNullable(type, out var nullValue)) Crawl(nullValue);
        else if (IsList(type, out var element)) Crawl(element);
        else if (IsDictionary(type, out var key, out var value))
        {
            Crawl(key);
            Crawl(value);
        }
        else
        {
            CrawlProperties(type);
            CrawlBaseType(type);
        }
    }

    private void CrawlProperties (Type type)
    {
        foreach (var prop in type.GetProperties())
            Crawl(prop.PropertyType);
    }

    private void CrawlBaseType (Type type)
    {
        if (type.BaseType != null)
            Crawl(type.BaseType);
    }

    private TypeMeta Inspect (Type type, InteropKind ik)
    {
        return serde.Inspect(type) ?? itd.Inspect(type, ik) ?? new TypeMeta(type);
    }
}
