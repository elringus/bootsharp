using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class TypeSyntaxBuilder (Preferences prefs)
{
    private NullabilityInfo? nullability;

    public string BuildArg (ArgumentMeta arg)
    {
        var nil = arg.Value.Nullable ? " | undefined" : "";
        return Build(arg.Value.Type.Clr, arg.Value.Nullability) + nil;
    }

    public string BuildReturn (MethodMeta method)
    {
        var nil = method.ReturnValue.Nullable ? " | null" : "";
        return Build(method.ReturnValue.Type.Clr, method.ReturnValue.Nullability) + nil;
    }

    public string Build (Type type, NullabilityInfo? nullability)
    {
        this.nullability = nullability;
        // nullability of topmost declarations is handled upstream (?/undefined/null)
        if (IsNullable(type, nullability, out var value)) type = value;
        return WithPrefs(prefs.Type, type.FullName!, Build(type));
    }

    private string Build (Type type)
    {
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
        if (type.IsGenericType)
        {
            EnterNullability();
            var args = string.Join(", ", type.GenericTypeArguments.Select(Build));
            return $"{BuildJSSpaceFullName(type, prefs)}<{args}>";
        }
        return BuildJSSpaceFullName(type, prefs);
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
