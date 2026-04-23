using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InterfaceInspector (MemberInspector members, string entryAssemblyName)
{
    private InteropKind interop;
    private (string space, string name, string full) impl;

    public InterfaceMeta Inspect (Type interfaceType, InteropKind interopKind)
    {
        interop = interopKind;
        impl = BuildInterfaceImpl(interfaceType, interop);
        return new InterfaceMeta {
            Interop = interop,
            Type = interfaceType,
            TypeSyntax = BuildSyntax(interfaceType),
            Namespace = impl.space,
            Name = impl.name,
            Members = [
                ..interfaceType.GetEvents().Select(CreateEvent),
                ..interfaceType.GetProperties().Where(ShouldInspectProperty).Select(CreateProperty),
                ..interfaceType.GetMethods().Where(ShouldInspectMethod).Select(CreateMethod)
            ]
        };
    }

    private bool ShouldInspectProperty (PropertyInfo prop)
    {
        if (prop.GetIndexParameters().Length != 0) return false;
        return prop.GetMethod?.IsAbstract == true || prop.SetMethod?.IsAbstract == true;
    }

    private bool ShouldInspectMethod (MethodInfo method)
    {
        return method.IsAbstract && !method.IsSpecialName;
    }

    private EventMeta CreateEvent (EventInfo info) => members.Inspect(info, interop) with {
        Assembly = entryAssemblyName,
        Space = impl.full
    };

    private PropertyMeta CreateProperty (PropertyInfo info) => members.Inspect(info, interop) with {
        Assembly = entryAssemblyName,
        Space = impl.full,
        CanGet = info.GetMethod?.IsAbstract == true,
        CanSet = info.SetMethod?.IsAbstract == true
    };

    private MethodMeta CreateMethod (MethodInfo info) => members.Inspect(info, interop) with {
        Assembly = entryAssemblyName,
        Space = impl.full
    };
}
