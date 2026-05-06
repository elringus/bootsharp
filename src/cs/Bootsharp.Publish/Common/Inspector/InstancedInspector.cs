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

    public IReadOnlyCollection<InstancedMeta> Collect ()
    {
        return byType.Values.ToArray();
    }

    private InstancedMeta InspectType (Type type, InteropKind ik) => new(type) {
        Interop = ik,
        Namespace = BuildInstanceSpace(type, ik),
        Name = BuildInstanceName(type),
        JSName = BuildInstanceJSName(type),
        Members = new List<MemberMeta>()
    };

    private InstancedMeta CollectMembers (InstancedMeta it)
    {
        var ik = it.Interop;
        var type = it.Clr;
        var cl = (List<MemberMeta>)it.Members;
        cl.AddRange(type.GetEvents().Select(m => members.Inspect(m, ik, it)));
        cl.AddRange(type.GetProperties().Where(ShouldInspectProperty).Select(m => members.Inspect(m, ik, it)));
        cl.AddRange(type.GetMethods().Where(ShouldInspectMethod).Select(m => members.Inspect(m, ik, it)));
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

    private string BuildInstanceSpace (Type type, InteropKind ik)
    {
        var space = "Bootsharp.Generated." + (ik == InteropKind.Export ? "Exports" : "Imports");
        if (type.Namespace != null) space += $".{type.Namespace}";
        return space;
    }

    private string BuildInstanceName (Type type)
    {
        var trimmed = type.IsInterface ? type.Name[1..] : type.Name;
        return "JS" + trimmed;
    }

    private string BuildInstanceJSName (Type type)
    {
        var name = BuildInstanceName(type);
        if (type.Namespace == null) return name;
        return $"{type.Namespace}.{name}".Replace(".", "_");
    }
}
