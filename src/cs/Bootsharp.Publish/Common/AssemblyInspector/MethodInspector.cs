using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MethodInspector (Preferences prefs, TypeConverter converter)
{
    public MethodMeta Inspect (MethodInfo info, MethodKind kind) => new() {
        Kind = kind,
        Assembly = info.DeclaringType!.Assembly.GetName().Name!,
        Space = info.DeclaringType.FullName!,
        Name = info.Name,
        Arguments = info.GetParameters().Select(CreateArgument).ToArray(),
        ReturnValue = new() {
            Type = info.ReturnType,
            TypeSyntax = BuildSyntax(info.ReturnType, info.ReturnParameter),
            JSTypeSyntax = converter.ToTypeScript(info.ReturnType, GetNullability(info.ReturnParameter)),
            Nullable = IsNullable(info),
            Async = IsTaskLike(info.ReturnType),
            Void = IsVoid(info.ReturnType),
            Serialized = ShouldSerialize(info.ReturnType)
        },
        JSSpace = BuildMethodSpace(info),
        JSName = WithPrefs(prefs.Function, info.Name, ToFirstLower(info.Name))
    };

    private ArgumentMeta CreateArgument (ParameterInfo info) => new() {
        Name = info.Name!,
        JSName = info.Name == "function" ? "fn" : info.Name!,
        Value = new() {
            Type = info.ParameterType,
            TypeSyntax = BuildSyntax(info.ParameterType, info),
            JSTypeSyntax = converter.ToTypeScript(info.ParameterType, GetNullability(info)),
            Nullable = IsNullable(info),
            Async = false,
            Void = false,
            Serialized = ShouldSerialize(info.ParameterType)
        }
    };

    private string BuildMethodSpace (MethodInfo info)
    {
        var space = info.DeclaringType!.Namespace ?? "";
        var name = BuildJSSpaceName(info.DeclaringType);
        if (info.DeclaringType.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
