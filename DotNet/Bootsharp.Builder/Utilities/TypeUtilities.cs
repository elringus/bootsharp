using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Bootsharp.Builder;

internal static class TypeUtilities
{
    public static bool IsAwaitable (Type type)
    {
        return type.GetMethod(nameof(Task.GetAwaiter)) != null;
    }

    public static bool IsList (Type type)
    {
        return type.IsArray || IsGenericList(type) || type.GetInterfaces().Any(IsGenericList);

        bool IsGenericList (Type type) => type.IsGenericType &&
                                          (type.GetGenericTypeDefinition().FullName == typeof(IList<>).FullName ||
                                           type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyList<>).FullName);
    }

    public static bool IsDictionary (Type type)
    {
        return IsDict(type) || type.GetInterfaces().Any(IsDict);

        bool IsDict (Type type) => type.IsGenericType &&
                                   (type.GetGenericTypeDefinition().FullName == typeof(IDictionary<,>).FullName ||
                                    type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyDictionary<,>).FullName);
    }

    public static Type GetListElementType (Type arrayType)
    {
        return arrayType.IsArray
            ? arrayType.GetElementType()!
            : arrayType.GenericTypeArguments[0];
    }

    public static NullabilityInfo GetNullability (PropertyInfo property)
    {
        return new NullabilityInfoContext().Create(property);
    }

    public static NullabilityInfo GetNullability (ParameterInfo parameter)
    {
        return new NullabilityInfoContext().Create(parameter);
    }

    public static bool IsNullable (PropertyInfo property)
    {
        if (IsNullable(property.PropertyType)) return true;
        return GetNullability(property).ReadState == NullabilityState.Nullable;
    }

    public static bool IsNullable (ParameterInfo parameter)
    {
        if (IsNullable(parameter.ParameterType)) return true;
        return GetNullability(parameter).ReadState == NullabilityState.Nullable;
    }

    public static bool IsNullable (MethodInfo method)
    {
        if (IsNullable(method.ReturnParameter)) return true;
        if (!IsAwaitable(method.ReturnType)) return false;
        return GetNullability(method.ReturnParameter).GenericTypeArguments
            .FirstOrDefault()?.ReadState == NullabilityState.Nullable;
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
        var assemblyPaths = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").ToList();
        foreach (var path in Directory.GetFiles(directory, "*.dll"))
            if (assemblyPaths.All(p => Path.GetFileName(p) != Path.GetFileName(path)))
                assemblyPaths.Add(path);
        var resolver = new PathAssemblyResolver(assemblyPaths);
        return new MetadataLoadContext(resolver);
    }

    public static bool ShouldIgnoreAssembly (string assemblyPath)
    {
        var assemblyName = Path.GetFileName(assemblyPath);
        return assemblyName.StartsWith("System.") ||
               assemblyName.StartsWith("Microsoft.") ||
               assemblyName.StartsWith("netstandard");
    }

    public static string GetGenericNameWithoutArgs (Type type)
    {
        var delimiterIndex = type.Name.IndexOf('`');
        return type.Name[..delimiterIndex];
    }
}
