using System;
using System.Threading.Tasks;

namespace Packer;

internal static class TypeConversion
{
    public static string ToTypeScript (Type type)
    {
        if (IsAwaitable(type)) return WithPromise(type);
        if (type == typeof(void)) return "void";
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single: return "number";
            case TypeCode.Boolean: return "boolean";
            case TypeCode.Char:
            case TypeCode.String: return "string";
            case TypeCode.DateTime: return "Date";
            default: return "any";
        }
    }

    public static bool IsAwaitable (Type type)
    {
        return type.GetMethod(nameof(Task.GetAwaiter)) != null;
    }

    private static string WithPromise (Type type)
    {
        if (type.GenericTypeArguments.Length == 0) return "Promise<void>";
        var resultType = ToTypeScript(type.GenericTypeArguments[0]);
        return $"Promise<{resultType}>";
    }
}
