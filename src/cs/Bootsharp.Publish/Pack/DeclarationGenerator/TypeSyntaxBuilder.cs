using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class TypeSyntaxBuilder (Preferences prefs)
{
    private NullabilityInfo? nullability;

    public string BuildName (Type type)
    {
        var full = BuildFullName(type);
        var dotIdx = full.LastIndexOf('.');
        return dotIdx > 0 ? full[(dotIdx + 1)..] : full;
    }

    public string BuildFullName (Type type)
    {
        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
        return Build(type, null);
    }

    public string BuildArg (ParameterInfo param)
    {
        if (param.Member.DeclaringType!.IsGenericType)
            param = param.Member.DeclaringType.GetGenericTypeDefinition()
                .GetMethod(param.Member.Name)!.GetParameters()[param.Position];
        var nul = GetNullability(param);
        var post = IsNullable(param.ParameterType, nul) ? " | undefined" : "";
        return Build(param.ParameterType, nul) + post;
    }

    public string BuildArg (EventInfo evt, ParameterInfo param)
    {
        var nul = GetNullability(evt, param);
        var post = IsNullable(param.ParameterType, nul) ? " | undefined" : "";
        return Build(param.ParameterType, nul) + post;
    }

    public string BuildReturn (MethodInfo method)
    {
        if (method.DeclaringType!.IsGenericType)
            method = method.DeclaringType.GetGenericTypeDefinition().GetMethod(method.Name)!;
        var nul = GetNullability(method.ReturnParameter);
        var post = IsNullable(method.ReturnType, nul) ? " | null" : "";
        return Build(method.ReturnType, nul) + post;
    }

    public string BuildProperty (PropertyInfo prop)
    {
        if (prop.DeclaringType!.IsGenericType)
            prop = prop.DeclaringType.GetGenericTypeDefinition().GetProperty(prop.Name)!;
        var nul = GetNullability(prop);
        var pre = IsNullable(prop.PropertyType, nul) ? "?: " : ": ";
        return pre + Build(prop.PropertyType, nul);
    }

    public string BuildVariable (PropertyInfo prop)
    {
        var nul = GetNullability(prop);
        var post = IsNullable(prop.PropertyType, nul) ? " | undefined" : "";
        return Build(prop.PropertyType, nul) + post;
    }

    private string Build (Type type, NullabilityInfo? nullability)
    {
        this.nullability = nullability;
        // nullability of topmost declarations is handled downstream (?/undefined/null)
        if (IsNullable(type, nullability, out var value)) type = value;
        return WithPrefs(prefs.Type, type.FullName!, Build(type));
    }

    private string Build (Type type)
    {
        if (type.IsGenericTypeParameter) return type.Name;
        if (IsNullable(type, out var nullValue)) return BuildNullable(nullValue);
        if (IsTaskLike(type)) return BuildTask(type);
        if (IsList(type, out var element)) return BuildList(type, element);
        if (IsDictionary(type, out var key, out var value)) return BuildDictionary(key, value);
        if (IsUserType(type)) return BuildUser(type);
        return BuildPrimitive(type);
    }

    private string BuildNullable (Type value)
    {
        EnterNullability();
        return $"{Build(value)} | null";
    }

    private string BuildTask (Type type)
    {
        var nil = EnterNullability() ? " | null" : "";
        if (!IsTaskWithResult(type, out var result)) return $"Promise<void>{nil}";
        return $"Promise<{Build(result)}{nil}>";
    }

    private string BuildList (Type type, Type element)
    {
        if (EnterNullability()) return $"Array<{Build(element)} | null>";
        if (!type.IsArray) return $"Array<{Build(element)}>";
        return Type.GetTypeCode(element) switch {
            TypeCode.Byte => "Uint8Array",
            TypeCode.SByte => "Int8Array",
            TypeCode.UInt16 => "Uint16Array",
            TypeCode.Int16 => "Int16Array",
            TypeCode.UInt32 => "Uint32Array",
            TypeCode.Int32 => "Int32Array",
            TypeCode.Int64 => "BigInt64Array",
            TypeCode.Single => "Float32Array",
            TypeCode.Double => "Float64Array",
            _ => $"Array<{Build(element)}>"
        };
    }

    private string BuildDictionary (Type key, Type value)
    {
        var nil = EnterNullability(1) ? " | null" : "";
        return $"Map<{Build(key)}, {Build(value)}{nil}>";
    }

    private string BuildUser (Type type)
    {
        var space = BuildJSSpace(type, prefs);
        var name = TrimGeneric(type.Name);
        var full = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        if (!type.IsGenericType) return full;
        EnterNullability();
        var args = string.Join(", ", type.GetGenericArguments().Select(Build));
        return $"{full}<{args}>";
    }

    private string BuildPrimitive (Type type)
    {
        if (IsVoid(type)) return "void";
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

    private bool EnterNullability (int idx = 0)
    {
        if (nullability == null) return false;
        if (nullability.GenericTypeArguments.Length > idx) nullability = nullability.GenericTypeArguments[idx];
        else if (nullability.ElementType != null) nullability = nullability.ElementType;
        else
        {
            nullability = null;
            return false;
        }
        return nullability.ReadState == NullabilityState.Nullable;
    }
}
