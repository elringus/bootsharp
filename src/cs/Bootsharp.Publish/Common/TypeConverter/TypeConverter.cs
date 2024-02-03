using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class TypeConverter (Preferences prefs)
{
    public IReadOnlyCollection<Type> CrawledTypes => crawler.Crawled;

    private readonly TypeCrawler crawler = new();
    private NullabilityInfo? nullability;

    public string ToTypeScript (Type type, NullabilityInfo? nullability)
    {
        this.nullability = nullability;
        // nullability of topmost type declarations is evaluated outside (method/property info)
        if (IsNullable(type)) type = GetNullableUnderlyingType(type);
        return WithPrefs(prefs.Type, type.FullName!, Convert(type));
    }

    private string Convert (Type type)
    {
        crawler.Crawl(type);
        if (IsNullable(type)) return ConvertNullable(type);
        if (IsList(type)) return ConvertList(type);
        if (IsDictionary(type)) return ConvertDictionary(type);
        if (IsTaskLike(type)) return ConvertAwaitable(type);
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
        if (EnterNullability()) return $"Array<{Convert(elementType)} | null>";
        return Type.GetTypeCode(elementType) switch {
            TypeCode.Byte => "Uint8Array",
            TypeCode.SByte => "Int8Array",
            TypeCode.UInt16 => "Uint16Array",
            TypeCode.Int16 => "Int16Array",
            TypeCode.UInt32 => "Uint32Array",
            TypeCode.Int32 => "Int32Array",
            TypeCode.Int64 => "BigInt64Array",
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
        EnterNullability();
        if (type.GenericTypeArguments.Length == 0) return "Promise<void>";
        return $"Promise<{Convert(type.GenericTypeArguments[0])}>";
    }

    private string ConvertGeneric (Type type)
    {
        EnterNullability();
        var args = string.Join(", ", type.GenericTypeArguments.Select(Convert));
        return $"{BuildJSSpaceFullName(type, prefs)}<{args}>";
    }

    private string ConvertFinal (Type type)
    {
        if (type.Name == "Void") return "void";
        if (CrawledTypes.Contains(type)) return BuildJSSpaceFullName(type, prefs);
        if (IsNumber(type)) return "number";
        return Type.GetTypeCode(type) switch {
            TypeCode.Int64 => "bigint",
            TypeCode.Char or TypeCode.String => "string",
            TypeCode.Boolean => "boolean",
            TypeCode.DateTime => "Date",
            _ => "any"
        };
    }

    private bool IsNumber (Type type) => Type.GetTypeCode(type) is
        TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or
        TypeCode.Int16 or TypeCode.Int32 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single;

    private bool EnterNullability ()
    {
        if (nullability == null) return false;
        if (nullability.GenericTypeArguments.Length > 0) nullability = nullability.GenericTypeArguments[0];
        else if (nullability.ElementType != null) nullability = nullability.ElementType;
        else return false;
        return nullability.ReadState == NullabilityState.Nullable;
    }
}
