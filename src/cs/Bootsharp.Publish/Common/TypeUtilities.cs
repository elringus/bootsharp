global using static Bootsharp.Publish.TypeUtilities;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal static class TypeUtilities
{
    private static readonly FrozenSet<string> native = new[] {
        typeof(string).FullName!, typeof(bool).FullName!, typeof(byte).FullName!,
        typeof(char).FullName!, typeof(short).FullName!, typeof(long).FullName!,
        typeof(int).FullName!, typeof(float).FullName!, typeof(double).FullName!,
        typeof(nint).FullName!, typeof(Task).FullName!, typeof(DateTime).FullName!,
        typeof(DateTimeOffset).FullName!, typeof(Exception).FullName!
    }.ToFrozenSet();

    private static readonly FrozenSet<string> arrayNative = new[] {
        typeof(byte).FullName!, typeof(int).FullName!,
        typeof(double).FullName!, typeof(string).FullName!
    }.ToFrozenSet();

    public static bool IsTaskLike (Type type)
    {
        return type.GetMethod(nameof(Task.GetAwaiter)) != null;
    }

    public static bool IsTaskWithResult (Type type, [NotNullWhen(true)] out Type? result)
    {
        return (result = IsTaskLike(type) && type.GenericTypeArguments.Length == 1
            ? type.GenericTypeArguments[0] : null) != null;
    }

    public static string MarshalAmbiguous (string typeSyntax, bool @return)
    {
        var promise = typeSyntax.StartsWith("global::System.Threading.Tasks.Task<");
        if (promise) typeSyntax = typeSyntax[36..];
        var result =
            typeSyntax.StartsWith("global::System.DateTime") ? "JSType.Date" :
            typeSyntax.StartsWith("global::System.Int64") ? "JSType.BigInt" : "";
        if (result == "") return "";
        if (promise) result = $"JSType.Promise<{result}>";
        result = $"JSMarshalAs<{result}>";
        if (@return) result = $"return: {result}";
        return $"[{result}] ";
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
        return IsDict(type) || type.GetInterfaces().Any(IsDict);

        bool IsDict (Type type) =>
            type.IsGenericType &&
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

    public static MetadataLoadContext CreateLoadContext (string directory)
    {
        var assemblyPaths = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").ToList();
        foreach (var path in Directory.GetFiles(directory, "*.dll"))
            if (assemblyPaths.All(p => Path.GetFileName(p) != Path.GetFileName(path)))
                assemblyPaths.Add(path);
        var resolver = new PathAssemblyResolver(assemblyPaths);
        return new MetadataLoadContext(resolver);
    }

    public static bool ShouldIgnoreAssembly (string filePath)
    {
        var assemblyName = Path.GetFileName(filePath);
        return assemblyName.StartsWith("System.") ||
               assemblyName.StartsWith("Microsoft.") ||
               assemblyName.StartsWith("netstandard") ||
               assemblyName.StartsWith("mscorlib");
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

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    public static bool ShouldSerialize (Type type)
    {
        if (IsVoid(type)) return false;
        if (IsInstancedInteropInterface(type, out _)) return false;
        if (IsTaskWithResult(type, out var result))
            // TODO: Remove 'IsList(result)' when resolved: https://github.com/elringus/bootsharp/issues/138
            return IsList(result) || ShouldSerialize(result);
        var array = type.IsArray;
        if (array) type = type.GetElementType()!;
        if (IsNullable(type)) type = GetNullableUnderlyingType(type);
        if (array) return !arrayNative.Contains(type.FullName!);
        return !native.Contains(type.FullName!);
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

    public static string WithPrefs (IReadOnlyCollection<Preference> prefs, string input, string @default)
    {
        foreach (var pref in prefs)
            if (Regex.IsMatch(input, pref.Pattern))
                return Regex.Replace(input, pref.Pattern, pref.Replacement);
        return @default;
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
