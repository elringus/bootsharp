using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MemberInspector (Preferences prefs, TypeInspector types, SerializedInspector serde)
{
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
        Arguments = method.GetParameters().Select(CreateArgument).ToArray(),
        JSSpace = BuildJSSpace(method.DeclaringType),
        JSName = WithPrefs(prefs.Function, method.Name, ToFirstLower(method.Name)),
        Value = CreateValue(method.ReturnParameter.ParameterType, GetNullability(method.ReturnParameter)),
        Void = IsVoid(method.ReturnParameter.ParameterType),
        Async = IsTaskLike(method.ReturnParameter.ParameterType)
    };

    private ArgumentMeta CreateArgument (ParameterInfo param) => new(param) {
        Name = param.Name!,
        JSName = param.Name == "function" ? "fn" : param.Name!,
        Value = CreateValue(param.ParameterType, GetNullability(param))
    };

    private ValueMeta CreateValue (Type type, NullabilityInfo nil)
    {
        IsInstancedInteropInterface(type, out var instanceType);
        return new() {
            Type = types.Inspect(type),
            TypeSyntax = BuildSyntax(type, nil),
            Nullable = IsNullable(type, nil),
            Nullability = nil,
            Serialized = serde.Inspect(type),
            InstanceType = instanceType
        };
    }

    private string BuildJSSpace (Type type)
    {
        var space = type.Namespace ?? "";
        var name = BuildJSSpaceName(type);
        if (type.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
