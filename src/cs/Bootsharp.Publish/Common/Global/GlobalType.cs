global using static Bootsharp.Publish.GlobalType;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Bootsharp.Publish;

internal static class GlobalType
{
    public static bool IsStatic (Type type)
    {
        return type.IsAbstract && type.IsSealed;
    }

    public static bool IsRecord (Type type)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return type.GetMethod("<Clone>$", flags) != null;
    }

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

    public static NullabilityInfo GetNullity (PropertyInfo prop) => new NullabilityInfoContext().Create(prop);
    public static NullabilityInfo GetNullity (ParameterInfo param) => new NullabilityInfoContext().Create(param);
    public static NullabilityInfo GetNullity (EventInfo evt) => new NullabilityInfoContext().Create(evt);
    public static NullabilityInfo GetNullity (EventInfo evt, ParameterInfo param)
    {
        if (evt.EventHandlerType!.IsGenericType)
        {
            var arg = evt.EventHandlerType.GetGenericTypeDefinition()
                .GetMethod("Invoke")!.GetParameters()[param.Position].ParameterType;
            if (arg.IsGenericParameter)
                return GetNullity(evt).GenericTypeArguments[arg.GenericParameterPosition];
        }
        return GetNullity(param);
    }

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

    public static string BuildJSName (string name)
    {
        name = ToFirstLower(name);
        return name == "function" ? "fn" : name;
    }

    public static string PrependIdArg (string args)
    {
        if (string.IsNullOrEmpty(args)) return "_id";
        return $"_id, {args}";
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
            var name = TrimGeneric(ResolveTypeName(type));
            var typeArgs = string.Join(", ", args.Select((a, i) => BuildSyntax(a, nul?.GenericTypeArguments[i])));
            return $"global::{name}<{typeArgs}>";
        }

        static string ResolveTypeName (Type type)
        {
            if (type.IsNested) return $"{ResolveTypeName(type.DeclaringType!)}.{type.Name}";
            if (type.Namespace is null) return type.Name;
            return $"{type.Namespace}.{type.Name}";
        }
    }

    public static string TrimGeneric (string typeName)
    {
        var delimiterIndex = typeName.IndexOf('`');
        if (delimiterIndex < 0) return typeName;
        return typeName[..delimiterIndex];
    }

    public static string Export (ArgumentMeta arg) => Export(arg.Value, arg.Name);
    public static string Export (ValueMeta value, string exp) => Export(value.Type, exp);
    public static string Export (TypeMeta type, string exp)
    {
        if (type is InstancedMeta it)
            if (it.Interop == InteropKind.Export) return $"Instances.Export({exp})";
            else return $"((global::{it.FullName}){exp})._id";
        if (type is SerializedMeta sm) return $"Serializer.Serialize({exp}, SerializerContext.{sm.Id})";
        return exp;
    }

    public static string Import (ArgumentMeta arg) => Import(arg.Value, arg.Name);
    public static string Import (ValueMeta value, string exp) => Import(value.Type, exp);
    public static string Import (TypeMeta type, string exp)
    {
        if (type is InstancedMeta it)
            if (it.Interop == InteropKind.Export) return $"Instances.Exported<{it.Syntax}>({exp})";
            else return $"Instances.Import({exp}, static id => new global::{it.FullName}(id))";
        if (type is SerializedMeta sm) return $"Serializer.Deserialize({exp}, SerializerContext.{sm.Id})";
        return exp;
    }

    public static string ExportJS (ArgumentMeta arg) => ExportJS(arg.Value, arg.JSName);
    public static string ExportJS (ValueMeta value, string exp) => ExportJS(value.Type, exp);
    public static string ExportJS (TypeMeta type, string exp)
    {
        if (type is InstancedMeta it)
            if (it.Interop == InteropKind.Export) return $"{exp}._id";
            else if (it.Importer is { } importer) return $"{importer}({exp})";
            else return $"instances.import({exp})";
        if (type is SerializedMeta sm) return $"serialize({exp}, {sm.Id})";
        return exp;
    }

    public static string ImportJS (ArgumentMeta arg) => ImportJS(arg.Value, arg.JSName);
    public static string ImportJS (ValueMeta value, string exp) => ImportJS(value.Type, exp);
    public static string ImportJS (TypeMeta type, string exp)
    {
        if (type is InstancedMeta it)
            if (it.Interop == InteropKind.Import) return $"instances.imported({exp})";
            else return $"instances.export({exp}, id => new {it.JSName}(id))";
        if (type is SerializedMeta sm) return $"deserialize({exp}, {sm.Id})";
        return exp;
    }
}
