using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InterfaceInspector (Preferences prefs, TypeConverter converter, string entryAssemblyName)
{
    private readonly MethodInspector methodInspector = new(prefs, converter);

    public InterfaceMeta Inspect (Type interfaceType, InterfaceKind kind)
    {
        var impl = BuildInteropInterfaceImplementationName(interfaceType, kind);
        return new InterfaceMeta {
            Kind = kind,
            Type = interfaceType,
            TypeSyntax = BuildSyntax(interfaceType),
            Namespace = impl.space,
            Name = impl.name,
            Methods = interfaceType.GetMethods()
                .Where(m => m.IsAbstract)
                .Select(m => CreateMethod(m, kind, impl.full)).ToArray()
        };
    }

    private MethodMeta CreateMethod (MethodInfo info, InterfaceKind kind, string space)
    {
        var name = WithPrefs(prefs.Event, info.Name, info.Name);
        return methodInspector.Inspect(info, ResolveMethodKind(kind, info, name)) with {
            Assembly = entryAssemblyName,
            Space = space,
            Name = name,
            JSName = ToFirstLower(name),
            InterfaceName = info.Name
        };
    }

    private MethodKind ResolveMethodKind (InterfaceKind iKind, MethodInfo info, string implMethodName)
    {
        if (iKind == InterfaceKind.Export) return MethodKind.Invokable;
        // TODO: This assumes event methods are always renamed via prefs, which may not be the case.
        if (implMethodName != info.Name) return MethodKind.Event;
        return MethodKind.Function;
    }
}
