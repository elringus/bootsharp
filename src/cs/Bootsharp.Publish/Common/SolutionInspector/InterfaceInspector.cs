using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InterfaceInspector (Preferences prefs, MemberInspector members, string entryAssemblyName)
{
    private InteropKind interop;
    private (string space, string name, string full) impl;

    public InterfaceMeta Inspect (Type interfaceType, InteropKind interopKind)
    {
        interop = interopKind;
        impl = BuildInterfaceImplName(interfaceType, interop);
        return new InterfaceMeta {
            Interop = interop,
            Type = interfaceType,
            TypeSyntax = BuildSyntax(interfaceType),
            Namespace = impl.space,
            Name = impl.name,
            Members = interfaceType.GetProperties().Where(ShouldInspectProperty).Select(CreateProperty)
                .Concat(interfaceType.GetMethods().Where(ShouldInspectMethod).Select(CreateMethod)).ToArray()
        };
    }

    private bool ShouldInspectMethod (MethodInfo method)
    {
        return method.IsAbstract && !method.IsSpecialName;
    }

    private bool ShouldInspectProperty (PropertyInfo prop)
    {
        if (prop.GetIndexParameters().Length != 0) return false;
        return prop.GetMethod?.IsAbstract == true || prop.SetMethod?.IsAbstract == true;
    }

    private MemberMeta CreateMethod (MethodInfo info)
    {
        var name = WithPrefs(prefs.Event, info.Name, info.Name);
        var method = members.Inspect(info, interop) with {
            Assembly = entryAssemblyName,
            Space = impl.full,
            Name = name,
            JSName = ToFirstLower(name)
        };
        if (interop == InteropKind.Import && name != info.Name)
            return new EventMeta(method, info.Name);
        return method;
    }

    private MemberMeta CreateProperty (PropertyInfo info)
    {
        return members.Inspect(info, interop) with {
            Assembly = entryAssemblyName,
            Space = impl.full,
            CanGet = info.GetMethod?.IsAbstract == true,
            CanSet = info.SetMethod?.IsAbstract == true
        };
    }
}
