global using static Bootsharp.Publish.GlobalType;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

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

    public static bool IsList (Type type, [NotNullWhen(true)] out Type? element)
    {
        if (type.IsArray) element = type.GetElementType()!;
        else if (IsList(type)) element = type.GenericTypeArguments[0];
        else element = null;
        return element != null;

        static bool IsList (Type type) =>
            type.IsGenericType &&
            (type.GetGenericTypeDefinition().FullName == typeof(List<>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IList<>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyList<>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(ICollection<>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyCollection<>).FullName);
    }

    public static bool IsDictionary (Type type, [NotNullWhen(true)] out Type? key, [NotNullWhen(true)] out Type? value)
    {
        if (IsDictionary(type))
        {
            key = type.GenericTypeArguments[0];
            value = type.GenericTypeArguments[1];
        }
        else key = value = null;
        return key != null;

        static bool IsDictionary (Type type) =>
            type.IsGenericType &&
            (type.GetGenericTypeDefinition().FullName == typeof(Dictionary<,>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IDictionary<,>).FullName ||
             type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyDictionary<,>).FullName);
    }

    public static NullabilityInfo GetNullability (EventInfo evt) => new NullabilityInfoContext().Create(evt);
    public static NullabilityInfo GetNullability (PropertyInfo prop) => new NullabilityInfoContext().Create(prop);
    public static NullabilityInfo GetNullability (ParameterInfo param) => new NullabilityInfoContext().Create(param);

    public static bool IsNullable (Type type) => IsNullable(type, out _);
    public static bool IsNullable (Type type, NullabilityInfo? info) => IsNullable(type, info, out _);
    public static bool IsNullable (Type type, [NotNullWhen(true)] out Type? value) => IsNullable(type, null, out value);
    public static bool IsNullable (Type type, NullabilityInfo? info, [NotNullWhen(true)] out Type? value)
    {
        if (info?.ReadState == NullabilityState.Nullable) value = type;
        else if (type.IsGenericType && type.Name.Contains("Nullable`") && type.GenericTypeArguments.Length == 1)
            value = type.GenericTypeArguments[0];
        else value = null;
        return value != null;
    }

    public static bool IsAutoProperty (PropertyInfo prop)
    {
        var backingFieldName = $"<{prop.Name}>k__BackingField";
        var backingField = prop.DeclaringType!.GetField(backingFieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return backingField != null;
    }

    public static bool IsInstancedInteropInterface (Type type, [NotNullWhen(true)] out Type? instanceType)
    {
        if (IsTaskWithResult(type, out instanceType))
            return IsInstancedInteropInterface(instanceType, out instanceType);
        return (instanceType = type.IsInterface && IsUserType(type) ? type : null) != null;
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
        return type.IsGenericType ? TrimGenericArgs(type.Name) : type.Name;
    }

    public static string BuildJSSpaceFullName (Type type, Preferences prefs)
    {
        var space = BuildJSSpace(type, prefs);
        var name = BuildJSSpaceName(type);
        return string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
    }

    public static (string space, string name, string full) BuildInterfaceImpl (Type instanceType, InteropKind interop)
    {
        var space = "Bootsharp.Generated." + (interop == InteropKind.Export ? "Exports" : "Imports");
        if (instanceType.Namespace != null) space += $".{instanceType.Namespace}";
        var name = "JS" + instanceType.Name[1..];
        return (space, name, $"{space}.{name}");
    }

    public static string PrependIdArg (string args)
    {
        if (string.IsNullOrEmpty(args)) return "_id";
        return $"_id, {args}";
    }

    public static string BuildJSInstanceClassName (InterfaceMeta it)
    {
        return it.FullName.Replace("Bootsharp.Generated.Exports.", "").Replace(".", "_");
    }

    public static string BuildSerializedId (Type type)
    {
        var builder = new StringBuilder();
        foreach (var c in BuildSyntax(type).Replace("global::", ""))
            if (char.IsLetterOrDigit(c) || c == '_') builder.Append(c);
            else if (c == '.') builder.Append('_');
            else if (c == '?') builder.Append("OrNull");
            else if (c == '[') builder.Append("Array");
            else if (c == '<') builder.Append("_Of_");
            else if (c == ',') builder.Append("_And_");
        return builder.ToString();
    }

    public static string BuildSyntax (Type type, NullabilityInfo? nul = null, bool forceNil = false)
    {
        var nil = (forceNil || nul?.ReadState == NullabilityState.Nullable) ? "?" : "";
        if (IsVoid(type)) return "void";
        if (type.IsArray) return $"{BuildSyntax(type.GetElementType()!, nul?.ElementType)}[]{nil}";
        if (type.IsGenericType) return BuildGeneric(type, type.GenericTypeArguments);
        return $"global::{ResolveTypeName(type)}{nil}";

        string BuildGeneric (Type type, Type[] args)
        {
            if (IsNullable(type, out var value)) return BuildSyntax(value, nul, true);
            var name = TrimGenericArgs(ResolveTypeName(type));
            var typeArgs = string.Join(", ", args.Select((a, i) => BuildSyntax(a, nul?.GenericTypeArguments[i])));
            return $"global::{name}<{typeArgs}>";
        }

        static string ResolveTypeName (Type type)
        {
            if (type.Namespace is null) return type.Name;
            return $"{type.Namespace}.{type.Name}";
        }
    }

    public static string TrimGenericArgs (string typeName)
    {
        var delimiterIndex = typeName.IndexOf('`');
        return typeName[..delimiterIndex];
    }
}
