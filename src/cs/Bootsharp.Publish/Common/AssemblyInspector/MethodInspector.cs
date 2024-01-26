using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MethodInspector (Preferences prefs, TypeConverter converter, string entryAssemblyName)
{
    private readonly InterfaceInspector interfaceInspector = new(prefs, converter, entryAssemblyName);
    private MethodInfo info = null!;
    private MethodKind kind;

    public (MethodMeta method, InterfaceMeta[] instanced) Inspect (MethodInfo info, MethodKind kind)
    {
        this.info = info;
        this.kind = kind;
        return (CreateMethod(), CreateInstanced());
    }

    private MethodMeta CreateMethod () => new() {
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
        JSSpace = BuildMethodSpace(),
        JSName = WithPrefs(prefs.Function, info.Name, ToFirstLower(info.Name))
    };

    private InterfaceMeta[] CreateInstanced ()
    {
        var inst = info.GetParameters().Where(IsInstanced).ToArray();
        if (inst.Length == 0) return [];
        var iKind = kind == MethodKind.Invokable ? InterfaceKind.Export : InterfaceKind.Import;
        return inst.SelectMany(t => interfaceInspector.Inspect(t.ParameterType, iKind, true)).ToArray();
    }

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

    private string BuildMethodSpace ()
    {
        var space = info.DeclaringType!.Namespace ?? "";
        var name = BuildJSSpaceName(info.DeclaringType);
        if (info.DeclaringType.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }

    private bool IsInstanced (ParameterInfo arg)
    {
        var type = arg.ParameterType;
        if (!type.IsInterface || string.IsNullOrEmpty(type.Namespace)) return false;
        return !type.Namespace.StartsWith("System.", StringComparison.Ordinal);
    }
}
