global using static Bootsharp.Publish.GlobalType;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp.Publish;

internal static class GlobalType
{
    public static bool IsTaskLike (Type type)
    {
        return type.GetMethod(nameof(Task.GetAwaiter)) != null;
    }

    public static bool IsTaskWithResult (Type type, [NotNullWhen(true)] out Type? result)
    {
        return (result = IsTaskLike(type) && type.GenericTypeArguments.Length == 1
            ? type.GenericTypeArguments[0] : null) != null;
    }

    public static bool IsVoid (Type type)
    {
        return type.FullName == "System.Void";
    }

    public static bool IsList (Type type)
    {
        return type.IsArray || IsGenericList(type) || type.GetInterfaces().Any(IsGenericList);

        bool IsGenericList (Type type) =>
            type.IsGenericType &&
            (type.GetGenericTypeDefinition().FullName == typeof(IList<>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyList<>).FullName);
    }

    public static bool IsDictionary (Type type)
    {
        return IsGenericDictionary(type) || type.GetInterfaces().Any(IsGenericDictionary);

        bool IsGenericDictionary (Type type) =>
            type.IsGenericType &&
            (type.GetGenericTypeDefinition().FullName == typeof(IDictionary<,>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyDictionary<,>).FullName);
    }

    public static bool IsCollection (Type type)
    {
        return type.IsInterface && type.IsGenericType &&
               (type.GetGenericTypeDefinition().FullName == typeof(ICollection<>).FullName ||
                type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyCollection<>).FullName);
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
        if (!IsTaskLike(method.ReturnType)) return false;
        return GetNullability(method.ReturnParameter).GenericTypeArguments
            .FirstOrDefault()?.ReadState == NullabilityState.Nullable;
    }

    public static bool IsNullable (Type type)
    {
        return type.IsGenericType &&
               type.Name.Contains("Nullable`") &&
               type.GenericTypeArguments.Length == 1;
    }

    public static Type GetNullableUnderlyingType (Type type)
    {
        return type.GenericTypeArguments[0];
    }

    public static bool IsAutoProperty (PropertyInfo property)
    {
        var backingFieldName = $"<{property.Name}>k__BackingField";
        var backingField = property.DeclaringType!.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return backingField != null;
    }

    public static string GetGenericNameWithoutArgs (string typeName)
    {
        var delimiterIndex = typeName.IndexOf('`');
        return typeName[..delimiterIndex];
    }

    public static bool IsInstancedInteropInterface (Type type, [NotNullWhen(true)] out Type? instanceType)
    {
        if (IsTaskWithResult(type, out instanceType))
            return IsInstancedInteropInterface(instanceType, out _);
        instanceType = type;
        if (!type.IsInterface) return false;
        if (string.IsNullOrEmpty(type.Namespace)) return true;
        return !type.Namespace.StartsWith("System.", StringComparison.Ordinal);
    }

    public static string BuildJSSpace (Type type, Preferences prefs)
    {
        var space = type.Namespace ?? "";
        if (type.IsNested)
        {
            if (!string.IsNullOrEmpty(space)) space += ".";
            space += type.DeclaringType!.Name;
        }
        return WithPrefs(prefs.Space, space, space);
    }

    public static string BuildJSSpaceName (Type type)
    {
        return type.IsGenericType ? GetGenericNameWithoutArgs(type.Name) : type.Name;
    }

    public static string BuildJSSpaceFullName (Type type, Preferences prefs)
    {
        var space = BuildJSSpace(type, prefs);
        var name = BuildJSSpaceName(type);
        return string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
    }

    public static (string space, string name, string full) BuildInteropInterfaceImplementationName (Type instanceType, InterfaceKind kind)
    {
        var space = "Bootsharp.Generated." + (kind == InterfaceKind.Export ? "Exports" : "Imports");
        if (instanceType.Namespace != null) space += $".{instanceType.Namespace}";
        var name = "JS" + instanceType.Name[1..];
        return (space, name, $"{space}.{name}");
    }

    public static string PrependInstanceIdArgName (string args)
    {
        if (string.IsNullOrEmpty(args)) return "_id";
        return $"_id, {args}";
    }

    public static string PrependInstanceIdArgTypeAndName (string args)
    {
        return $"{BuildSyntax(typeof(int))} {PrependInstanceIdArgName(args)}";
    }

    public static string BuildJSInteropInstanceClassName (InterfaceMeta inter)
    {
        return inter.FullName.Replace("Bootsharp.Generated.Exports.", "").Replace(".", "_");
    }

    public static string BuildSyntax (Type type) => BuildSyntax(type, null, false);

    public static string BuildSyntax (Type type, ParameterInfo info) => BuildSyntax(type, GetNullability(info));

    private static string BuildSyntax (Type type, NullabilityInfo? nul, bool forceNil = false)
    {
        var nil = (forceNil || nul?.ReadState == NullabilityState.Nullable) ? "?" : "";
        if (IsVoid(type)) return "void";
        if (type.IsArray) return $"{BuildSyntax(GetListElementType(type), nul?.ElementType)}[]{nil}";
        if (type.IsGenericType) return BuildGeneric(type, type.GenericTypeArguments);
        return $"global::{ResolveTypeName(type)}{nil}";

        string BuildGeneric (Type type, Type[] args)
        {
            if (IsNullable(type)) return BuildSyntax(args[0], nul, true);
            var name = GetGenericNameWithoutArgs(ResolveTypeName(type));
            var typeArgs = string.Join(", ", args.Select((a, i) => BuildSyntax(a, nul?.GenericTypeArguments[i])));
            return $"global::{name}<{typeArgs}>";
        }

        static string ResolveTypeName (Type type)
        {
            if (type.Namespace is null) return type.Name;
            return $"{type.Namespace}.{type.Name}";
        }
    }
}
