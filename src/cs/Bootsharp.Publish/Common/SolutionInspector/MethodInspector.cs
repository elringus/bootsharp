using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MethodInspector (Preferences prefs, TypeConverter converter)
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
        ReturnValue = CreateValue(method.ReturnParameter, true),
        JSSpace = BuildMethodSpace(),
        JSName = WithPrefs(prefs.Function, method.Name, ToFirstLower(method.Name))
    };

    private ArgumentMeta CreateArgument (ParameterInfo param) => new() {
        Name = param.Name!,
        JSName = param.Name == "function" ? "fn" : param.Name!,
        Value = CreateValue(param, false)
    };

    private ValueMeta CreateValue (ParameterInfo param, bool @return) => new() {
        Type = param.ParameterType,
        TypeSyntax = BuildSyntax(param.ParameterType, param),
        JSTypeSyntax = converter.ToTypeScript(param.ParameterType, GetNullability(param)),
        TypeInfo = BuildTypeInfo(param.ParameterType),
        Nullable = @return ? IsNullable(method) : IsNullable(param),
        Async = @return && IsTaskLike(param.ParameterType),
        Void = @return && IsVoid(param.ParameterType),
        Serialized = ShouldSerialize(param.ParameterType),
        Instance = IsInstancedInteropInterface(param.ParameterType, out var instanceType),
        InstanceType = instanceType
    };

    private string BuildMethodSpace ()
    {
        var space = method.DeclaringType!.Namespace ?? "";
        var name = BuildJSSpaceName(method.DeclaringType);
        if (method.DeclaringType.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
