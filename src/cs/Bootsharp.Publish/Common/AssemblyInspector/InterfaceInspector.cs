using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InterfaceInspector (Preferences prefs, TypeConverter converter, string entryAssemblyName)
{
    private readonly MethodInspector methodInspector = new(prefs, converter, entryAssemblyName);
    private readonly List<InterfaceMeta> metas = [];

    public InterfaceMeta[] Inspect (Type interfaceType, InterfaceKind kind, bool instanced)
    {
        metas.Clear();
        metas.Add(CreateInterface(interfaceType, kind, instanced));
        return metas.ToArray();
    }

    private InterfaceMeta CreateInterface (Type interfaceType, InterfaceKind kind, bool instanced)
    {
        var space = "Bootsharp.Generated." + (kind == InterfaceKind.Export ? "Exports" : "Imports");
        if (interfaceType.Namespace != null) space += $".{interfaceType.Namespace}";
        var name = "JS" + interfaceType.Name[1..];
        return new InterfaceMeta {
            Kind = kind,
            Instanced = instanced,
            Type = interfaceType,
            TypeSyntax = BuildSyntax(interfaceType),
            Namespace = space,
            Name = name,
            Methods = interfaceType.GetMethods().Select(m => CreateMethod(m, kind, $"{space}.{name}")).ToArray()
        };
    }

    private InterfaceMethodMeta CreateMethod (MethodInfo info, InterfaceKind iKind, string space)
    {
        var name = WithPrefs(prefs.Event, info.Name, info.Name);
        var mKind = iKind == InterfaceKind.Export ? MethodKind.Invokable
            : name != info.Name ? MethodKind.Event : MethodKind.Function;
        var (method, instanced) = methodInspector.Inspect(info, mKind);
        metas.AddRange(instanced);
        return new() {
            Name = info.Name,
            Generated = method with {
                Assembly = entryAssemblyName,
                Space = space,
                Name = name,
                JSName = ToFirstLower(name)
            }
        };
    }
}
