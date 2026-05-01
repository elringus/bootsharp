using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MemberInspector (Preferences prefs, TypeInspector types, SerializedInspector serde)
{
    public EventMeta Inspect (EventInfo evt, InteropKind interop)
    {
        var inv = evt.EventHandlerType!.GetMethod("Invoke")!;
        return new(evt) {
            Interop = interop,
            Assembly = evt.DeclaringType!.Assembly.GetName().Name!,
            Space = evt.DeclaringType.FullName!,
            Name = evt.Name,
            Arguments = inv.GetParameters().Select((p, i) => CreateArg(p, GetArgNullability(p, i))).ToArray(),
            JSSpace = BuildJSSpace(evt.DeclaringType),
            JSName = WithPrefs(prefs.Function, evt.Name, ToFirstLower(evt.Name)),
            Value = CreateValue(inv.ReturnParameter.ParameterType, GetNullability(inv.ReturnParameter))
        };

        NullabilityInfo GetArgNullability (ParameterInfo param, int index)
        {
            if (evt.EventHandlerType!.IsGenericType)
            {
                var genType = evt.EventHandlerType.GetGenericTypeDefinition()
                    .GetMethod("Invoke")!.GetParameters()[index].ParameterType;
                if (genType.IsGenericParameter)
                    return GetNullability(evt).GenericTypeArguments[genType.GenericParameterPosition];
            }
            return GetNullability(param);
        }
    }

    public PropertyMeta Inspect (PropertyInfo prop, InteropKind interop) => new(prop) {
        Interop = interop,
        Assembly = prop.DeclaringType!.Assembly.GetName().Name!,
        Space = prop.DeclaringType.FullName!,
        JSSpace = BuildJSSpace(prop.DeclaringType),
        Name = prop.Name,
        JSName = ToFirstLower(prop.Name),
        Value = CreateValue(prop.PropertyType, GetNullability(prop)),
        CanGet = prop.GetMethod != null,
        CanSet = prop.SetMethod != null
    };

    public MethodMeta Inspect (MethodInfo method, InteropKind interop) => new(method) {
        Interop = interop,
        Assembly = method.DeclaringType!.Assembly.GetName().Name!,
        Space = method.DeclaringType.FullName!,
        Name = method.Name,
        Arguments = method.GetParameters().Select(p => CreateArg(p, GetNullability(p))).ToArray(),
        JSSpace = BuildJSSpace(method.DeclaringType),
        JSName = WithPrefs(prefs.Function, method.Name, ToFirstLower(method.Name)),
        Value = CreateValue(method.ReturnParameter.ParameterType, GetNullability(method.ReturnParameter)),
        Void = IsVoid(method.ReturnParameter.ParameterType),
        Async = IsTaskLike(method.ReturnParameter.ParameterType)
    };

    private ArgumentMeta CreateArg (ParameterInfo param, NullabilityInfo nil) => new(param) {
        Name = param.Name!,
        JSName = param.Name == "function" ? "fn" : param.Name!,
        Value = CreateValue(param.ParameterType, nil)
    };

    private ValueMeta CreateValue (Type type, NullabilityInfo nil)
    {
        IsInstancedInterface(type, out var instanceType);
        return new() {
            Type = types.Inspect(type),
            TypeSyntax = BuildSyntax(type, nil),
            Nullable = IsNullable(type, nil),
            Nullability = nil,
            Serialized = serde.Inspect(type),
            InstanceType = instanceType
        };
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
