using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InterfaceInspector (MemberInspector members, string entryAssemblyName)
{
    private InteropKind interop;
    private string memberSpace = null!;

    public InterfaceMeta Inspect (Type interfaceType, InteropKind interopKind)
    {
        var space = BuildInterfaceSpace(interfaceType, interopKind);
        var name = BuildInterfaceName(interfaceType);
        return new InterfaceMeta {
            Interop = interop = interopKind,
            Type = interfaceType,
            TypeSyntax = BuildSyntax(interfaceType),
            Namespace = space,
            Name = name,
            FullName = memberSpace = $"{space}.{name}",
            JSName = BuildInterfaceJSName(interfaceType),
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
        Space = memberSpace
    };

    private PropertyMeta CreateProperty (PropertyInfo info) => members.Inspect(info, interop) with {
        Assembly = entryAssemblyName,
        Space = memberSpace,
        CanGet = info.GetMethod?.IsAbstract == true,
        CanSet = info.SetMethod?.IsAbstract == true
    };

    private MethodMeta CreateMethod (MethodInfo info) => members.Inspect(info, interop) with {
        Assembly = entryAssemblyName,
        Space = memberSpace
    };

    private static string BuildInterfaceSpace (Type type, InteropKind interop)
    {
        var space = "Bootsharp.Generated." + (interop == InteropKind.Export ? "Exports" : "Imports");
        if (type.Namespace != null) space += $".{type.Namespace}";
        return space;
    }

    private static string BuildInterfaceName (Type type)
    {
        return "JS" + type.Name[1..];
    }

    private static string BuildInterfaceJSName (Type type)
    {
        var name = BuildInterfaceName(type);
        if (type.Namespace == null) return name;
        return $"{type.Namespace}.{name}".Replace(".", "_");
    }
}
