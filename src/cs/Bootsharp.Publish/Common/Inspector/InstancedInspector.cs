using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class InstancedInspector (MemberInspector members)
{
    private readonly Dictionary<Type, InstancedMeta> byType = [];

    public InstancedMeta? Inspect (Type type, InteropKind ik)
    {
        if (byType.TryGetValue(type, out var meta)) return meta;
        if (IsTaskWithResult(type, out var result)) return Inspect(result, ik);
        if (!IsInstancedType(type)) return null;
        return CollectMembers(byType[type] = InspectType(type, ik));
    }

    public ModuleMeta? InspectModule (Type type, InteropKind ik)
    {
        if (ik == InteropKind.Import && !type.IsInterface || IsStatic(type)) return null;
        var it = CollectMembers(InspectType(type, ik));
        return new(type) { Interop = ik, Namespace = it.Namespace, Name = it.Name, Members = it.Members };
    }

    public IReadOnlyCollection<InstancedMeta> Collect ()
    {
        return byType.Values.ToArray();
    }

    private InstancedMeta InspectType (Type type, InteropKind ik) => new(type) {
        Interop = ik,
        Namespace = BuildSpace(type, ik),
        Name = BuildName(type),
        JSName = BuildJSName(type),
        Members = new List<MemberMeta>()
    };

    private InstancedMeta CollectMembers (InstancedMeta it)
    {
        var cl = (List<MemberMeta>)it.Members;
        cl.AddRange(it.Clr.GetEvents().Select(m => members.Inspect(m, it.Interop)));
        cl.AddRange(it.Clr.GetProperties().Where(ShouldInspectProperty).Select(m => members.Inspect(m, it.Interop)));
        cl.AddRange(it.Clr.GetMethods().Where(ShouldInspectMethod).Select(m => members.Inspect(m, it.Interop)));
        return it;
    }

    private bool ShouldInspectProperty (PropertyInfo prop)
    {
        if (prop.GetIndexParameters().Length != 0) return false;
        if (prop.DeclaringType!.IsInterface)
            return prop.GetMethod?.IsAbstract == true ||
                   prop.SetMethod?.IsAbstract == true;
        return true;
    }

    private bool ShouldInspectMethod (MethodInfo method)
    {
        if (method.IsSpecialName) return false;
        if (method.DeclaringType!.FullName == typeof(object).FullName) return false;
        if (method.DeclaringType!.IsInterface) return method.IsAbstract;
        return !method.IsStatic;
    }

    private string BuildSpace (Type type, InteropKind ik)
    {
        var space = "Bootsharp.Generated." + (ik == InteropKind.Export ? "Exports" : "Imports");
        if (type.Namespace != null) space += $".{type.Namespace}";
        return space;
    }

    private string BuildName (Type type)
    {
        var trimmed = type.IsInterface ? type.Name[1..] : type.Name;
        return "JS" + trimmed;
    }

    private string BuildJSName (Type type)
    {
        var name = BuildName(type);
        if (type.Namespace == null) return name;
        return $"{type.Namespace}.{name}".Replace(".", "_");
    }
}
