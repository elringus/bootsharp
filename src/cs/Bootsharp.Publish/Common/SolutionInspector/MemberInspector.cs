using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MemberInspector (Preferences prefs, TypeInspector types,
    SerializedInspector serde, InstancedInspector instanced)
{
    public EventMeta Inspect (EventInfo evt, InteropKind ik, InstancedMeta? host)
    {
        return new(evt) {
            Interop = ik,
            Space = BuildSpace(evt.DeclaringType!, host),
            JSSpace = BuildJSSpace(evt.DeclaringType!),
            Name = evt.Name,
            JSName = WithPrefs(prefs.Function, evt.Name, ToFirstLower(evt.Name)),
            Arguments = evt.EventHandlerType!.GetMethod("Invoke")!.GetParameters()
                .Select((p, i) => CreateArg(p, GetArgNullability(p, i), ik)).ToArray()
        };

        NullabilityInfo GetArgNullability (ParameterInfo param, int idx)
        {
            if (evt.EventHandlerType!.IsGenericType)
            {
                var genType = evt.EventHandlerType.GetGenericTypeDefinition()
                    .GetMethod("Invoke")!.GetParameters()[idx].ParameterType;
                if (genType.IsGenericParameter)
                    return GetNullability(evt).GenericTypeArguments[genType.GenericParameterPosition];
            }
            return GetNullability(param);
        }
    }

    public PropertyMeta Inspect (PropertyInfo prop, InteropKind ik, InstancedMeta? host)
    {
        return new PropertyMeta(prop) {
            Interop = ik,
            Space = BuildSpace(prop.DeclaringType!, host),
            JSSpace = BuildJSSpace(prop.DeclaringType!),
            Name = prop.Name,
            JSName = ToFirstLower(prop.Name),
            GetValue = CreateValue(prop.GetMethod, ik),
            SetValue = CreateValue(prop.SetMethod, ik.Invert()),
        };

        ValueMeta? CreateValue (MethodInfo? method, InteropKind ik)
        {
            if (method is null) return null;
            if (prop.DeclaringType!.IsInterface && !method.IsAbstract) return null;
            return this.CreateValue(prop.PropertyType, GetNullability(prop), ik);
        }
    }

    public MethodMeta Inspect (MethodInfo method, InteropKind ik, InstancedMeta? host) => new(method) {
        Interop = ik,
        Space = BuildSpace(method.DeclaringType!, host),
        JSSpace = BuildJSSpace(method.DeclaringType!),
        Name = method.Name,
        JSName = WithPrefs(prefs.Function, method.Name, ToFirstLower(method.Name)),
        Arguments = method.GetParameters().Select(p => CreateArg(p, GetNullability(p), ik.Invert())).ToArray(),
        Return = CreateValue(method.ReturnParameter.ParameterType, GetNullability(method.ReturnParameter), ik),
        Void = IsVoid(method.ReturnParameter.ParameterType),
        Async = IsTaskLike(method.ReturnParameter.ParameterType)
    };

    private ArgumentMeta CreateArg (ParameterInfo param, NullabilityInfo nil, InteropKind ik) => new(param) {
        Name = param.Name!,
        JSName = param.Name == "function" ? "fn" : param.Name!,
        Value = CreateValue(param.ParameterType, nil, ik)
    };

    private ValueMeta CreateValue (Type type, NullabilityInfo nil, InteropKind ik) => new() {
        Type = types.Inspect(type),
        TypeSyntax = BuildSyntax(type, nil),
        Nullable = IsNullable(type, nil),
        Nullability = nil,
        Serialized = serde.Inspect(type),
        Instanced = instanced.Inspect(type, ik, this)
    };

    private string BuildSpace (Type decl, InstancedMeta? host)
    {
        if (host != null) return host.FullName;
        return decl.FullName!;
    }

    private string BuildJSSpace (Type decl)
    {
        var space = decl.Namespace ?? "";
        var name = TrimGeneric(decl.Name);
        if (decl.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
