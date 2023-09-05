using System.Reflection;

namespace Bootsharp.Builder;

internal sealed class TypeConverter(NamespaceBuilder spaceBuilder)
{
    public IReadOnlyCollection<Type> CrawledTypes => crawler.Crawled;

    private readonly TypeCrawler crawler = new();
    private NullabilityInfo? nullability;

    public string ToTypeScript (Type type) => ToTypeScript(type, null);

    public string ToTypeScript (Type type, NullabilityInfo? nullability)
    {
        this.nullability = nullability;
        // nullability of topmost type declarations is evaluated outside (method/property info)
        if (IsNullable(type)) type = GetNullableUnderlyingType(type);
        return Convert(type);
    }

    private string Convert (Type type)
    {
        crawler.Crawl(type);
        if (IsNullable(type)) return ConvertNullable(type);
        if (IsList(type)) return ConvertList(type);
        if (IsDictionary(type)) return ConvertDictionary(type);
        if (IsAwaitable(type)) return ConvertAwaitable(type);
        if (type.IsGenericType && CrawledTypes.Contains(type)) return ConvertGeneric(type);
        return ConvertFinal(type);
    }

    private string ConvertNullable (Type type)
    {
        return $"{Convert(GetNullableUnderlyingType(type))} | null";
    }

    private string ConvertList (Type type)
    {
        var elementType = GetListElementType(type);
        if (EnterNullability(type)) return $"Array<{Convert(elementType)} | null>";
        return Type.GetTypeCode(elementType) switch {
            TypeCode.Byte => "Uint8Array",
            TypeCode.SByte => "Int8Array",
            TypeCode.UInt16 => "Uint16Array",
            TypeCode.Int16 => "Int16Array",
            TypeCode.UInt32 => "Uint32Array",
            TypeCode.Int32 => "Int32Array",
            _ => $"Array<{Convert(elementType)}>"
        };
    }

    private string ConvertDictionary (Type type)
    {
        var keyType = type.GenericTypeArguments[0];
        var valueType = type.GenericTypeArguments[1];
        return $"Map<{Convert(keyType)}, {Convert(valueType)}>";
    }

    private string ConvertAwaitable (Type type)
    {
        EnterNullability(type);
        if (type.GenericTypeArguments.Length == 0) return "Promise<void>";
        return $"Promise<{Convert(type.GenericTypeArguments[0])}>";
    }

    private string ConvertGeneric (Type type)
    {
        EnterNullability(type);
        var args = string.Join(", ", type.GenericTypeArguments.Select(Convert));
        return $"{spaceBuilder.Build(type)}.{GetGenericNameWithoutArgs(type)}<{args}>";
    }

    private string ConvertFinal (Type type)
    {
        if (type.Name == "Void") return "void";
        if (CrawledTypes.Contains(type)) return $"{spaceBuilder.Build(type)}.{type.Name}";
        return Type.GetTypeCode(type) switch {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or
                TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single => "number",
            TypeCode.Char or TypeCode.String => "string",
            TypeCode.Boolean => "boolean",
            TypeCode.DateTime => "Date",
            _ => "any"
        };
    }

    private bool EnterNullability (Type type)
    {
        if (nullability is null) return false;
        var nullable = nullability.ElementType?.ReadState == NullabilityState.Nullable ||
                       nullability.GenericTypeArguments.FirstOrDefault()?.ReadState == NullabilityState.Nullable;
        if (type.IsArray) nullability = nullability.ElementType;
        else nullability = nullability.GenericTypeArguments.FirstOrDefault();
        return nullable;
    }
}
