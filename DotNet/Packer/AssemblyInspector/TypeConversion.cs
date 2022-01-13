using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Packer;

internal static class TypeConversion
{
    public static string ToTypeScript (Type type)
    {
        if (type.Name == "Void") return "void";
        if (IsArray(type)) return ToArray(type);
        if (IsAwaitable(type)) return ToPromise(type);
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

    public static bool ShouldIgnoreAssembly (string assemblyPath)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        if (assemblyName.StartsWith("System.")) return true;
        if (assemblyName.StartsWith("Microsoft.")) return true;
        if (assemblyName.StartsWith("TypeScriptModelsGenerator")) return true;
        return false;
    }

    public static bool ShouldConvertToObjectType (Type type)
    {
        if (Type.GetTypeCode(type) != TypeCode.Object && !type.IsEnum) return false;
        return !ShouldIgnoreAssembly(type.Assembly.FullName) ||
               IsArray(type) && ShouldConvertToObjectType(GetArrayElementType(type));
    }

    public static string ConvertToObjectType (Type type, ObjectTypeGenerator typeGenerator)
    {
        if (IsArray(type))
        {
            var elementType = GetArrayElementType(type);
            return $"Array<{ConvertToObjectType(elementType, typeGenerator)}>";
        }
        typeGenerator.AddObjectType(type);
        return type.Name;
    }

    public static bool IsAwaitable (Type type)
    {
        return type.GetMethod(nameof(Task.GetAwaiter)) != null;
    }

    public static bool IsArray (Type type)
    {
        return type.IsArray ||
               type.IsGenericType && type.GetInterfaces().Any(i => i.Name == "IList");
    }

    public static Type GetArrayElementType (Type arrayType)
    {
        return arrayType.IsArray
            ? arrayType.GetElementType()
            : arrayType.GenericTypeArguments[0];
    }

    private static string ToArray (Type type)
    {
        var elementType = GetArrayElementType(type);
        return $"Array<{ToTypeScript(elementType)}>";
    }

    private static string ToPromise (Type type)
    {
        if (type.GenericTypeArguments.Length == 0) return "Promise<void>";
        var resultType = ToTypeScript(type.GenericTypeArguments[0]);
        return $"Promise<{resultType}>";
    }
}
