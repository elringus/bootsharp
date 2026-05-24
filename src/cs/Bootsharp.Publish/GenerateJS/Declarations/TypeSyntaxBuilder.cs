using System.Reflection;

namespace Bootsharp.Publish;

/// <summary>
/// Builds TypeScript type syntax.
/// </summary>
internal sealed class TypeSyntaxBuilder (JSModules mds)
{
    private JSModule module = null!;

    public void EnterModule (JSModule module)
    {
        this.module = module;
    }

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
        var nul = GetNullity(param);
        if (param.HasDefaultValue) return $"?: {Build(param.ParameterType, nul)}";
        var post = IsNullable(param.ParameterType, nul) ? " | undefined" : "";
        return $": {Build(param.ParameterType, nul)}{post}";
    }

    public string BuildArg (EventInfo evt, ParameterInfo param)
    {
        var nul = GetNullity(evt, param);
        var post = IsNullable(param.ParameterType, nul) ? " | undefined" : "";
        return $": {Build(param.ParameterType, nul)}{post}";
    }

    public string BuildReturn (MethodInfo method)
    {
        if (method.DeclaringType!.IsGenericType)
            method = method.DeclaringType.GetGenericTypeDefinition().GetMethod(method.Name)!;
        var nul = GetNullity(method.ReturnParameter);
        var post = IsNullable(method.ReturnType, nul) ? " | null" : "";
        return Build(method.ReturnType, nul) + post;
    }

    public string BuildProperty (PropertyInfo prop)
    {
        if (prop.DeclaringType!.IsGenericType)
            prop = prop.DeclaringType.GetGenericTypeDefinition().GetProperty(prop.Name)!;
        var nul = GetNullity(prop);
        var pre = IsNullable(prop.PropertyType, nul) ? "?: " : ": ";
        return pre + Build(prop.PropertyType, nul);
    }

    public string BuildVariable (PropertyInfo prop)
    {
        var nul = GetNullity(prop);
        var post = IsNullable(prop.PropertyType, nul) ? " | undefined" : "";
        return Build(prop.PropertyType, nul) + post;
    }

    private string Build (Type type, NullabilityInfo? nul)
    {
        if (type.IsGenericTypeParameter) return type.Name;
        if (IsNullable(type, out var inner)) return Build(inner, EnterNullity(nul));
        if (IsTaskLike(type)) return BuildTask(type, nul);
        if (IsUserType(type)) return BuildUser(type, nul);
        if (IsList(type, out var element)) return BuildList(type, element, nul);
        if (IsDictionary(type, out var key, out var value)) return BuildDictionary(key, value, nul);
        return BuildPrimitive(type);
    }

    private string BuildTask (Type type, NullabilityInfo? nul)
    {
        nul = EnterNullity(nul);
        var nil = IsNullable(nul) ? " | null" : "";
        if (!IsTaskWithResult(type, out var result)) return $"Promise<void>{nil}";
        return $"Promise<{Build(result, nul)}{nil}>";
    }

    private string BuildList (Type type, Type element, NullabilityInfo? nul)
    {
        nul = EnterNullity(nul);
        if (IsNullable(element, nul)) return $"Array<{Build(element, nul)} | null>";
        if (!type.IsArray) return $"Array<{Build(element, nul)}>";
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
            _ => $"Array<{Build(element, nul)}>"
        };
    }

    private string BuildDictionary (Type key, Type value, NullabilityInfo? nul)
    {
        nul = EnterNullity(nul, 1);
        var nil = IsNullable(value, nul) ? " | null" : "";
        return $"Map<{Build(key, null)}, {Build(value, nul)}{nil}>";
    }

    private string BuildUser (Type type, NullabilityInfo? nul)
    {
        var @ref = mds.Ref(type, module);
        if (!type.IsGenericType) return @ref;
        var argTypes = type.GetGenericArguments();
        var args = new List<string>(argTypes.Length);
        for (var i = 0; i < argTypes.Length; i++)
        {
            var argNul = EnterNullity(nul, i);
            var nil = IsNullable(argTypes[i], argNul) ? " | undefined" : "";
            args.Add($"{Build(argTypes[i], argNul)}{nil}");
        }
        var name = @ref[..@ref.IndexOf("_Of_", StringComparison.Ordinal)];
        var disc = argTypes.Length > 1 ? argTypes.Length.ToString() : "";
        return $"{name}{disc}<{string.Join(", ", args)}>";
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

    private static NullabilityInfo? EnterNullity (NullabilityInfo? nul, int idx = 0)
    {
        if (nul == null) return null;
        if (nul.GenericTypeArguments.Length > idx) return nul.GenericTypeArguments[idx];
        return nul.ElementType;
    }
}
