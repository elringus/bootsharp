using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Packer;

internal static class TypeUtilities
{
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
            ? arrayType.GetElementType()!
            : arrayType.GenericTypeArguments[0];
    }

    public static bool IsNullable (PropertyInfo property)
    {
        if (IsNullable(property.PropertyType)) return true;
        var context = new NullabilityInfoContext().Create(property);
        return context.ReadState == NullabilityState.Nullable;
    }

    public static bool IsNullable (ParameterInfo parameter)
    {
        if (IsNullable(parameter.ParameterType)) return true;
        var context = new NullabilityInfoContext().Create(parameter);
        return context.ReadState == NullabilityState.Nullable;
    }

    public static bool IsNullable (MethodInfo method)
    {
        if (IsNullable(method.ReturnParameter)) return true;
        if (!IsAwaitable(method.ReturnType)) return false;
        var context = new NullabilityInfoContext().Create(method.ReturnParameter);
        return context.GenericTypeArguments.FirstOrDefault()?.ReadState == NullabilityState.Nullable;
    }

    public static bool IsNullable (Type type)
    {
        return type.IsGenericType &&
               type.Name.Contains("Nullable`") &&
               type.GetGenericArguments().Length == 1;
    }

    public static Type GetNullableUnderlyingType (Type type)
    {
        return type.GetGenericArguments()[0];
    }

    public static bool IsAutoProperty (PropertyInfo property)
    {
        var backingFieldName = $"<{property.Name}>k__BackingField";
        var backingField = property.DeclaringType!.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return backingField != null;
    }

    public static MetadataLoadContext CreateLoadContext (string directory)
    {
        var assemblyPaths = Directory.GetFiles(directory, "*.dll");
        var resolver = new PathAssemblyResolver(assemblyPaths);
        return new MetadataLoadContext(resolver);
    }

    public static bool ShouldIgnoreAssembly (string assemblyPath)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        return assemblyName.StartsWith("System.") ||
               assemblyName.StartsWith("Microsoft.") ||
               assemblyName.StartsWith("TypeScriptModelsGenerator");
    }

    public static string GetGenericNameWithoutArgs (Type type)
    {
        var delimiterIndex = type.Name.IndexOf('`');
        return type.Name[..delimiterIndex];
    }
}
