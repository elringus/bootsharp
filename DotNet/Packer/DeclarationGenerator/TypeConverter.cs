using System;
using System.Collections.Generic;
using System.Linq;
using static Packer.TypeUtilities;

namespace Packer;

internal class TypeConverter
{
    private readonly HashSet<Type> objectTypes = new();
    private readonly NamespaceBuilder spaceBuilder;

    public TypeConverter (NamespaceBuilder spaceBuilder)
    {
        this.spaceBuilder = spaceBuilder;
    }

    public string ToTypeScript (Type type)
    {
        if (ShouldConvertToObject(type)) return ConvertToObject(type);
        return ConvertToSimple(type);
    }

    public List<Type> GetObjectTypes () => objectTypes.ToList();

    private bool ShouldConvertToObject (Type type)
    {
        type = GetUnderlyingType(type);
        return (Type.GetTypeCode(type) == TypeCode.Object || type.IsEnum) &&
               !ShouldIgnoreAssembly(type.Assembly.FullName!);
    }

    private string ConvertToObject (Type type)
    {
        if (IsDictionary(type) && GetDictionaryElementType(type) is var types) return $"Map<{ToTypeScript(types.KeyType)}, {ToTypeScript(types.ValueType)}>";
        if (IsArray(type)) return $"Array<{ConvertToObject(GetArrayElementType(type))}>";
        if (IsNullable(type)) return ConvertToObject(GetNullableUnderlyingType(type));
        CrawlObjectType(type);
        var name = type.IsGenericType ? ToGeneric(type) : type.Name;
        return $"{spaceBuilder.Build(type)}.{name}";
    }

    private string ConvertToSimple (Type type)
    {
        if (IsDictionary(type) && GetDictionaryElementType(type) is var types) return $"Map<{ToTypeScript(types.KeyType)}, {ToTypeScript(types.ValueType)}>";
        if (type.Name == "Void") return "void";
        if (IsArray(type)) return ToArray(type);
        if (IsAwaitable(type)) return ToPromise(type);
        if (IsNullable(type)) return ConvertToSimple(GetNullableUnderlyingType(type));
        return ConvertTypeCode(Type.GetTypeCode(type));
    }

    private string ToGeneric (Type type)
    {
        var args = string.Join(", ", type.GenericTypeArguments.Select(ToTypeScript));
        return $"{GetGenericNameWithoutArgs(type)}<{args}>";
    }

    private string ToArray (Type type)
    {
        var elementType = GetArrayElementType(type);
        return Type.GetTypeCode(elementType) switch {
            TypeCode.Byte => "Uint8Array",
            TypeCode.SByte => "Int8Array",
            TypeCode.UInt16 => "Uint16Array",
            TypeCode.Int16 => "Int16Array",
            TypeCode.UInt32 => "Uint32Array",
            TypeCode.Int32 => "Int32Array",
            _ => $"Array<{ConvertToSimple(elementType)}>"
        };
    }

    private string ToPromise (Type type)
    {
        if (type.GenericTypeArguments.Length == 0) return "Promise<void>";
        var resultType = ConvertToSimple(type.GenericTypeArguments[0]);
        return $"Promise<{resultType}>";
    }

    private string ConvertTypeCode (TypeCode typeCode) => typeCode switch {
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
        type = GetUnderlyingType(type);
        if (!objectTypes.Add(type)) return;
        CrawlProperties(type);
        CrawlBaseType(type);
    }

    private void CrawlProperties (Type type)
    {
        var propertyTypesToAdd = type.GetProperties()
            .Select(m => m.PropertyType)
            .Where(ShouldConvertToObject);
        foreach (var propertyType in propertyTypesToAdd)
            CrawlObjectType(propertyType);
    }

    private void CrawlBaseType (Type type)
    {
        if (type.BaseType != null && ShouldConvertToObject(type.BaseType))
            CrawlObjectType(type.BaseType);
    }

    private Type GetUnderlyingType (Type type)
    {
        if (IsNullable(type)) return GetNullableUnderlyingType(type);
        if (IsDictionary(type)) return type;
        if (IsArray(type)) return GetArrayElementType(type);
        return type;
    }
}
