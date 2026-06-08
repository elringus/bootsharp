global using static Bootsharp.Publish.GlobalType;
global using Nullity = System.Reflection.NullabilityInfo;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

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

    public static bool IsDelegate (Type type)
    {
        for (var t = type.BaseType; t != null; t = t.BaseType)
            if (t.FullName == "System.MulticastDelegate")
                return true;
        return false;
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
            (OpenGeneric(type).FullName == typeof(List<>).FullName ||
             OpenGeneric(type).FullName == typeof(IList<>).FullName ||
             OpenGeneric(type).FullName == typeof(IReadOnlyList<>).FullName ||
             OpenGeneric(type).FullName == typeof(ICollection<>).FullName ||
             OpenGeneric(type).FullName == typeof(IReadOnlyCollection<>).FullName);
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
            (OpenGeneric(type).FullName == typeof(Dictionary<,>).FullName ||
             OpenGeneric(type).FullName == typeof(IDictionary<,>).FullName ||
             OpenGeneric(type).FullName == typeof(IReadOnlyDictionary<,>).FullName);
    }

    public static bool IsSameType (Type a, Type b)
    {
        return OpenGeneric(a) == OpenGeneric(b);
    }

    public static Nullity GetNullity (EventInfo evt) => new NullabilityInfoContext().Create(evt);
    public static Nullity GetNullity (EventInfo evt, ParameterInfo param) =>
        GetNullity(param, new NullabilityInfoContext().Create(evt));
    public static Nullity GetNullity (PropertyInfo prop) =>
        FixNullity(new NullabilityInfoContext().Create(prop), prop.CustomAttributes, prop.DeclaringType);
    public static Nullity GetNullity (ParameterInfo param, Nullity? nul = null)
    {
        var self = FixNullity(new NullabilityInfoContext().Create(param), param.CustomAttributes, param.Member);
        if (nul?.GenericTypeArguments is not { Length: > 0 } args) return self;
        // resolve nullability from the open (unbound) generic form of the method
        var method = param.Member.Module.ResolveMethod(((MethodBase)param.Member).MetadataToken)!;
        param = param.Position < 0 ? ((MethodInfo)method).ReturnParameter : method.GetParameters()[param.Position];
        if (!param.ParameterType.IsGenericParameter) return self;
        if (IsNullable(GetNullity(param))) return self;
        return args[param.ParameterType.GenericParameterPosition];
    }

    public static bool IsNullable (Nullity? nul) => nul?.ReadState == NullabilityState.Nullable;
    public static bool IsNullable (Type type, Nullity? nul) => IsNullable(type, nul, out _);
    public static bool IsNullable (Type type, [NotNullWhen(true)] out Type? value) => IsNullable(type, null, out value);
    public static bool IsNullable (Type type, Nullity? nul, [NotNullWhen(true)] out Type? value)
    {
        if (type.IsGenericType && type.Name.Contains("Nullable`") && type.GenericTypeArguments.Length == 1)
            value = type.GenericTypeArguments[0];
        else if (IsNullable(nul)) value = type;
        else value = null;
        return value != null;
    }

    public static string PrependIdArg (string args)
    {
        if (string.IsNullOrEmpty(args)) return "_id";
        return $"_id, {args}";
    }

    public static string BuildId (Type type, Nullity? nul = null)
    {
        var sb = new StringBuilder();
        foreach (var c in BuildSyntax(type, nul).Replace("global::", ""))
            if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
            else if (c == '.') sb.Append('_');
            else if (c == '?') sb.Append("OrNull");
            else if (c == '[') sb.Append("Array");
            else if (c == '<') sb.Append("_Of_");
            else if (c == ',') sb.Append("_And_");
        return sb.ToString();
    }

    public static string BuildSyntax (Type type, Nullity? nul = null)
    {
        var nil = IsNullable(nul) ? "?" : "";
        if (IsVoid(type)) return "void";
        if (type.IsArray) return $"{BuildSyntax(type.GetElementType()!, nul?.ElementType)}[]{nil}";
        if (type.IsGenericType) return BuildGeneric(type, type.GenericTypeArguments);
        return $"global::{ResolveTypeName(type)}{nil}";

        string BuildGeneric (Type type, Type[] args)
        {
            if (IsNullable(type, out var value))
                return BuildSyntax(value, value.IsGenericType ? nul : null) + "?";
            var name = TrimGeneric(ResolveTypeName(type));
            var typeArgs = string.Join(", ", args.Select((a, i) =>
                BuildSyntax(a, nul?.GenericTypeArguments[i])));
            return $"global::{name}<{typeArgs}>";
        }

        static string ResolveTypeName (Type type)
        {
            if (type.IsNested) return $"{ResolveTypeName(type.DeclaringType!)}.{type.Name}";
            return type.Namespace is null ? type.Name : $"{type.Namespace}.{type.Name}";
        }
    }

    public static string TrimGeneric (string typeName)
    {
        return Regex.Replace(typeName, @"`\d+(\[\[.*\]\])?", "");
    }

    public static Type OpenGeneric (Type type)
    {
        return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
    }

    public static string ExportCS (ArgumentMeta arg) => ExportCS(arg.Value, arg.Name);
    public static string ExportCS (ValueMeta value, string exp) => ExportCS(value.Type, exp);
    public static string ExportCS (TypeMeta type, string exp)
    {
        if (type is InstanceMeta) return $"Instances.Export({exp})";
        if (type is SerializedMeta sm) return $"Serializer.Serialize({exp}, SerializerContext.{sm.Id})";
        return exp;
    }

    public static string ImportCS (ArgumentMeta arg) => ImportCS(arg.Value, arg.Name);
    public static string ImportCS (ValueMeta value, string exp) => ImportCS(value.Type, exp);
    public static string ImportCS (TypeMeta type, string exp)
    {
        if (type is InstanceMeta it) return $"Instances.Resolve<{it.Syntax}>({exp})";
        if (type is SerializedMeta sm) return $"Serializer.Deserialize({exp}, SerializerContext.{sm.Id})";
        return exp;
    }

    public static string ExportJS (ArgumentMeta arg) // arguments are undefined (not null) by convention
        => ExportJS(arg.Value, arg.JSName) + (arg.Value.Nullable ? " ?? undefined" : "");
    public static string ExportJS (ValueMeta value, string exp) => ExportJS(value.Type, exp);
    public static string ExportJS (TypeMeta type, string exp)
    {
        if (type is InstanceMeta it) return $"$i.resolve({exp}, $i.{it.Id})";
        if (type is SerializedMeta sm) return $"deserialize({exp}, $s.{sm.Id})";
        return exp;
    }

    public static string ImportJS (ArgumentMeta arg) => ImportJS(arg.Value, arg.JSName);
    public static string ImportJS (ValueMeta value, string exp) => ImportJS(value.Type, exp);
    public static string ImportJS (TypeMeta type, string exp)
    {
        if (type is InstanceMeta it)
            if (it.Importer is { } importer) return $"$i.{importer}({exp})";
            else return $"$i.import({exp})";
        if (type is SerializedMeta sm) return $"serialize({exp}, $s.{sm.Id})";
        return exp;
    }

    private static Nullity FixNullity (Nullity nul, IEnumerable<CustomAttributeData> attrs, MemberInfo? scope)
    {
        // Nullity is conservatively reported as nullable for unconstrained generic parameters; this workaround
        // resolves actual nullability via a compiler heuristic. https://github.com/dotnet/runtime/issues/115014

        if (!nul.Type.IsGenericTypeParameter) return nul;
        var state = IsAnnotated() ? NullabilityState.Nullable : NullabilityState.NotNull;
        SetReadState(nul, state);
        SetWriteState(nul, state);
        return nul;

        bool IsAnnotated ()
        {
            foreach (var attr in attrs)
                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute")
                    return (byte)attr.ConstructorArguments[0].Value! == 2;
            for (var type = scope; type != null; type = type.DeclaringType)
                foreach (var attr in type.CustomAttributes)
                    if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute")
                        return (byte)attr.ConstructorArguments[0].Value! == 2;
            return false;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ReadState")]
        static extern void SetReadState (Nullity nul, NullabilityState value);
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_WriteState")]
        static extern void SetWriteState (Nullity nul, NullabilityState value);
    }
}
