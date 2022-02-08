using System;
using System.Collections.Generic;
using System.Linq;
using static Packer.TypeUtilities;

namespace Packer;

internal class TypeConverter
{
    private readonly HashSet<Type> crawledTypes = new();

    public string ToTypeScript (Type type)
    {
        if (ShouldConvertToObject(type))
            return ConvertToObject(type);
        return ConvertToSimple(type);
    }

    public List<Type> GetCrawledTypes () => crawledTypes.ToList();

    private static bool ShouldConvertToObject (Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } nullable)
            return ShouldConvertToObject(nullable);
        if (IsArray(type) && ShouldConvertToObject(GetArrayElementType(type)))
            return ShouldConvertToObject(GetArrayElementType(type));
        return (Type.GetTypeCode(type) == TypeCode.Object || type.IsEnum) &&
               !ShouldIgnoreAssembly(type.Assembly.FullName);
    }

    private string ConvertToObject (Type type)
    {
        if (IsArray(type))
            return $"Array<{ConvertToObject(GetArrayElementType(type))}>";
        if (Nullable.GetUnderlyingType(type) is { } nullable)
            return ConvertToObject(nullable);
        CrawlObjectType(type);
        return type.Name;
    }

    private static string ConvertToSimple (Type type)
    {
        if (type.Name == "Void") return "void";
        if (IsArray(type)) return ToArray(type);
        if (IsAwaitable(type)) return ToPromise(type);
        if (Nullable.GetUnderlyingType(type) is { } nullable)
            return ConvertToSimple(nullable);
        return ConvertTypeCode(Type.GetTypeCode(type));
    }

    private static string ToArray (Type type)
    {
        var elementType = GetArrayElementType(type);
        return $"Array<{ConvertToSimple(elementType)}>";
    }

    private static string ToPromise (Type type)
    {
        if (type.GenericTypeArguments.Length == 0) return "Promise<void>";
        var resultType = ConvertToSimple(type.GenericTypeArguments[0]);
        return $"Promise<{resultType}>";
    }

    private static string ConvertTypeCode (TypeCode typeCode) => typeCode switch {
        TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or
            TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
            TypeCode.Decimal or TypeCode.Double or TypeCode.Single => "number",
        TypeCode.Char or TypeCode.String => "string",
        TypeCode.Boolean => "boolean",
        TypeCode.DateTime => "Date",
        _ => "any"
    };

    private void CrawlObjectType (Type type)
    {
        if (!crawledTypes.Add(type)) return;
        CrawlProperties(type);
        CrawlBaseType(type);
    }

    private void CrawlProperties (Type type)
    {
        var propertyTypesToAdd = type.GetProperties()
            .Select(m => m.PropertyType)
            .Where(ShouldConvertToObject);
        foreach (var propertyType in propertyTypesToAdd)
            CrawlObjectType(IsArray(propertyType) ? GetArrayElementType(propertyType) : propertyType);
    }

    private void CrawlBaseType (Type type)
    {
        if (type.BaseType != null && ShouldConvertToObject(type.BaseType))
            CrawlObjectType(type.BaseType);
    }
}
