using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InterfaceInspector (Preferences prefs, TypeConverter converter, string entryAssemblyName)
{
    private readonly MethodInspector methodInspector = new(prefs, converter);

    public InterfaceMeta Inspect (Type interfaceType, InterfaceKind kind)
    {
        var space = "Bootsharp.Generated." + (kind == InterfaceKind.Export ? "Exports" : "Imports");
        if (interfaceType.Namespace != null) space += $".{interfaceType.Namespace}";
        var name = "JS" + interfaceType.Name[1..];
        return new InterfaceMeta {
            Kind = kind,
            Type = interfaceType,
            TypeSyntax = BuildSyntax(interfaceType),
            Namespace = space,
            Name = name,
            Methods = interfaceType.GetMethods().Select(m => CreateMethod(m, kind, $"{space}.{name}")).ToArray()
        };
    }

    private MethodMeta CreateMethod (MethodInfo info, InterfaceKind iKind, string space)
    {
        var name = WithPrefs(prefs.Event, info.Name, info.Name);
        var mKind = iKind == InterfaceKind.Export ? MethodKind.Invokable
            : name != info.Name ? MethodKind.Event : MethodKind.Function;
        return methodInspector.Inspect(info, mKind) with {
            Assembly = entryAssemblyName,
            Space = space,
            Name = name,
            JSName = ToFirstLower(name),
            InterfaceName = info.Name
        };
    }
}
