using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class TypeInspector
{
    private readonly Dictionary<Type, TypeMeta> byType = [];

    public TypeMeta Inspect (ParameterInfo info)
    {
        return Crawl(info.ParameterType);
    }

    public IReadOnlyCollection<TypeMeta> Collect ()
    {
        return byType.Values.ToArray();
    }

    private TypeMeta Crawl (Type type)
    {
        if (byType.TryGetValue(type, out var meta)) return meta;
        meta = byType[type] = new(type);
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
        return meta;
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
}
