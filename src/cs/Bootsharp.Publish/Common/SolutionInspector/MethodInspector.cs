using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MethodInspector (Preferences prefs, TypeInspector types, SerializedInspector serde)
{
    private MethodInfo method = null!;
    private MethodKind kind;

    public MethodMeta Inspect (MethodInfo method, MethodKind kind)
    {
        this.method = method;
        this.kind = kind;
        return CreateMethod();
    }

    private MethodMeta CreateMethod () => new() {
        Kind = kind,
        Assembly = method.DeclaringType!.Assembly.GetName().Name!,
        Space = method.DeclaringType.FullName!,
        Name = method.Name,
        Arguments = method.GetParameters().Select(CreateArgument).ToArray(),
        JSSpace = BuildMethodSpace(),
        JSName = WithPrefs(prefs.Function, method.Name, ToFirstLower(method.Name)),
        ReturnValue = CreateValue(method.ReturnParameter),
        Void = IsVoid(method.ReturnParameter.ParameterType),
        Async = IsTaskLike(method.ReturnParameter.ParameterType)
    };

    private ArgumentMeta CreateArgument (ParameterInfo param) => new() {
        Name = param.Name!,
        JSName = param.Name == "function" ? "fn" : param.Name!,
        Value = CreateValue(param)
    };

    private ValueMeta CreateValue (ParameterInfo param)
    {
        var nullability = GetNullability(param);
        IsInstancedInteropInterface(param.ParameterType, out var instanceType);
        return new() {
            Type = types.Inspect(param),
            TypeSyntax = BuildSyntax(param.ParameterType, nullability),
            Nullable = IsNullable(param.ParameterType, nullability),
            Nullability = nullability,
            Serialized = serde.Inspect(param),
            InstanceType = instanceType
        };
    }

    private string BuildMethodSpace ()
    {
        var space = method.DeclaringType!.Namespace ?? "";
        var name = BuildJSSpaceName(method.DeclaringType);
        if (method.DeclaringType.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
