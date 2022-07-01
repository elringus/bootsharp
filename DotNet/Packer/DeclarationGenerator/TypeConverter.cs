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

    public string ToTypeScript(Type type) => ToTypeScript(type, true);

    public string ToTypeScript (Type type, bool withNamespace)
    {
        if (type.IsGenericParameter) return type.Name;
        if (ShouldConvertToObject(type)) return ConvertToObject(type, withNamespace);
        return ConvertToSimple(type);
    }

    public List<Type> GetObjectTypes () => objectTypes.ToList();

    private bool ShouldConvertToObject (Type type)
    {
        type = GetUnderlyingType(type);
        return (Type.GetTypeCode(type) == TypeCode.Object || type.IsEnum) &&
               !ShouldIgnoreAssembly(type.Assembly.FullName!);
    }

    private string ConvertToObject (Type type, bool withNamespace)
    {
        if (IsArray(type)) return $"Array<{ConvertToObject(GetArrayElementType(type), withNamespace)}>";
        if (IsNullable(type)) return ConvertToObject(GetNullableUnderlyingType(type), withNamespace);

        CrawlObjectType(type);

        string typeName = type.IsGenericType
            ? ConvertGenericType(type)
            : type.Name;
        
        return withNamespace
            ? $"{spaceBuilder.Build(type)}.{typeName}"
            : typeName;
    }

    public string ConvertGenericType (Type type)
    {
        CrawlObjectType(type);

        var genericTypeName = type.Name.Substring(0, type.Name.IndexOf("`"));
        var genericTypeArguments = string.Join(", ", type.GetGenericArguments().Select(ToTypeScript));
        
        return $"{genericTypeName}<{genericTypeArguments}>";
    }

    private string ConvertToSimple (Type type)
    {
        if (type.Name == "Void") return "void";
        if (IsArray(type)) return ToArray(type);
        if (IsAwaitable(type)) return ToPromise(type);
        if (IsNullable(type)) return ConvertToSimple(GetNullableUnderlyingType(type));
        return ConvertTypeCode(Type.GetTypeCode(type));
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

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            foreach (var argument in type.GenericTypeArguments)
            {
                if (ShouldConvertToObject(argument))
                    CrawlObjectType(argument);
            }

            var definition = type.GetGenericTypeDefinition();
            if (ShouldConvertToObject(definition))
                CrawlObjectType(definition);
        }

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
        if (IsArray(type)) return GetArrayElementType(type);
        return type;
    }
}
