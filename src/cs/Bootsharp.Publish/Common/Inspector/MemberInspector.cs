using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class MemberInspector (Preferences prefs, Func<Type, InteropKind, TypeMeta> inspect)
{
    public EventMeta Inspect (EventInfo evt, InteropKind ik) => new(evt) {
        Interop = ik,
        Space = evt.DeclaringType!.FullName!,
        JSSpace = BuildJSSpace(evt.DeclaringType!),
        Name = evt.Name,
        JSName = BuildJSName(evt.Name),
        Arguments = evt.EventHandlerType!.GetMethod("Invoke")!.GetParameters()
            .Select(p => CreateArg(p, GetNullability(evt, p), ik)).ToArray()
    };

    public PropertyMeta Inspect (PropertyInfo prop, InteropKind ik) => new(prop) {
        Interop = ik,
        Space = prop.DeclaringType!.FullName!,
        JSSpace = BuildJSSpace(prop.DeclaringType!),
        Name = prop.Name,
        JSName = BuildJSName(prop.Name),
        GetValue = prop.GetMethod != null ? CreateValue(prop.PropertyType, GetNullability(prop), ik) : null,
        SetValue = prop.SetMethod != null ? CreateValue(prop.PropertyType, GetNullability(prop), ik.Invert()) : null
    };

    public MethodMeta Inspect (MethodInfo method, InteropKind ik) => new(method) {
        Interop = ik,
        Space = method.DeclaringType!.FullName!,
        JSSpace = BuildJSSpace(method.DeclaringType!),
        Name = method.Name,
        JSName = WithPrefs(prefs.Function, method.Name, BuildJSName(method.Name)),
        Arguments = method.GetParameters().Select(p => CreateArg(p, GetNullability(p), ik.Invert())).ToArray(),
        Return = CreateValue(method.ReturnParameter.ParameterType, GetNullability(method.ReturnParameter), ik),
        Void = IsVoid(method.ReturnParameter.ParameterType),
        Async = IsTaskLike(method.ReturnParameter.ParameterType)
    };

    private ArgumentMeta CreateArg (ParameterInfo param, NullabilityInfo nil, InteropKind ik) => new(param) {
        Name = param.Name!,
        JSName = BuildJSName(param.Name!),
        Value = CreateValue(param.ParameterType, nil, ik)
    };

    private ValueMeta CreateValue (Type type, NullabilityInfo nil, InteropKind ik) => new() {
        Type = inspect(type, ik),
        TypeSyntax = BuildSyntax(type, nil),
        Nullable = IsNullable(type, nil),
        Nullity = nil
    };

    private string BuildJSSpace (Type decl)
    {
        var space = decl.Namespace ?? "";
        var name = TrimGeneric(decl.Name);
        if (decl.IsInterface) name = name[1..];
        var fullname = string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
        return WithPrefs(prefs.Space, fullname, fullname);
    }
}
