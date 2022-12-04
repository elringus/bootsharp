using System;
using System.Collections.Generic;
using System.Linq;
using static Packer.TypeUtilities;

namespace Packer;

internal class TypeCrawler
{
    public IReadOnlyCollection<Type> Crawled => crawled;

    private readonly HashSet<Type> crawled = new();

    public void Crawl (Type type)
    {
        if (!ShouldCrawl(type)) return;
        type = GetUnderlyingType(type);
        if (!crawled.Add(type)) return;
        CrawlProperties(type);
        CrawlBaseType(type);
    }

    private bool ShouldCrawl (Type type)
    {
        type = GetUnderlyingType(type);
        return (Type.GetTypeCode(type) == TypeCode.Object || type.IsEnum) &&
               !ShouldIgnoreAssembly(type.Assembly.FullName!);
    }

    private void CrawlProperties (Type type)
    {
        var propertyTypesToAdd = type.GetProperties()
            .Select(m => m.PropertyType)
            .Where(ShouldCrawl);
        foreach (var propertyType in propertyTypesToAdd)
            Crawl(propertyType);
    }

    private void CrawlBaseType (Type type)
    {
        if (type.BaseType != null && ShouldCrawl(type.BaseType))
            Crawl(type.BaseType);
    }

    private Type GetUnderlyingType (Type type)
    {
        if (IsNullable(type)) return GetNullableUnderlyingType(type);
        if (IsList(type)) return GetUnderlyingType(GetListElementType(type));
        return type;
    }
}
