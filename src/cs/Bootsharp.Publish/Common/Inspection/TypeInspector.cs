using System.Reflection;

namespace Bootsharp.Publish;

internal sealed class TypeInspector
{
    internal delegate InstanceMeta? InspectInstanced (Type type, InteropKind ik);

    private readonly Dictionary<(Type, InteropKind), InstanceMeta> its = [];
    private readonly Dictionary<Type, TypeMeta> crawled = [];
    private readonly HashSet<Type> inspectedModuleTypes = [];
    private readonly List<SurfaceMeta> surfaces = [];
    private readonly SerializedInspector srd;

    public TypeInspector ()
    {
        srd = new SerializedInspector(InspectInstance);
    }

    public void Inspect (Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
            if (InspectStatic(type) is { } st)
                surfaces.Add(st);
        foreach (var attr in assembly.CustomAttributes)
            if (ResolveIK(attr) is { } ik)
                foreach (var arg in (IEnumerable<CustomAttributeTypedArgument>)attr.ConstructorArguments[0].Value!)
                    if (InspectModule((Type)arg.Value!, ik) is { } md)
                        surfaces.Add(md);
    }

    public IReadOnlyCollection<TypeMeta> Collect ()
    {
        TypeMeta[] specialized = [..surfaces, ..its.Values, ..srd.Collect()];
        var clrs = specialized.Select(t => t.Clr).ToHashSet();
        return [..specialized, ..crawled.Values.Where(c => !clrs.Contains(c.Clr))];
    }

    private StaticMeta? InspectStatic (Type type)
    {
        if (type.Namespace?.StartsWith("Bootsharp.Generated") == true) return null;
        var members = new List<MemberMeta>();
        var st = new StaticMeta(type) { Members = members };
        var flags = BindingFlags.Public | BindingFlags.Static;
        foreach (var evt in type.GetEvents(flags))
            if (ResolveIK(evt) is { } ik)
                members.Add(InspectEvent(evt, ik, st));
        foreach (var prop in type.GetProperties(flags))
            if (ResolveIK(prop) is { } ik)
                members.Add(InspectProperty(prop, ik, st));
        foreach (var method in type.GetMethods(flags))
            if (ResolveIK(method) is { } ik)
                members.Add(InspectMethod(method, ik, st));
        return members.Count > 0 ? st : null;
    }

    private ModuleMeta? InspectModule (Type type, InteropKind ik)
    {
        if (!inspectedModuleTypes.Add(type) || IsStatic(type) ||
            ik == InteropKind.Import && !type.IsInterface) return null;
        var md = new ModuleMeta(type) {
            IK = ik,
            Proxy = BuildProxy(type, ik),
            Members = new List<MemberMeta>()
        };
        return InspectMembers(md, ik);
    }

    private InstanceMeta? InspectInstance (Type type, InteropKind ik)
    {
        if (its.TryGetValue((type, ik), out var it)) return it;
        if (IsTaskWithResult(type, out var result)) return InspectInstance(result, ik);
        if (!IsInstanced(type)) return null;
        // instances with events need specialized registrars to un-/sub them
        var special = type.GetEvents().Length > 0;
        it = its[(type, ik)] = new(type) {
            IK = ik,
            Proxy = BuildProxy(type, ik),
            Members = new List<MemberMeta>(),
            Exporter = special && ik == InteropKind.Export ? "Export" : null, // discriminated by types on C#
            Importer = special && ik == InteropKind.Import ? $"import_{BuildId(type)}" : null,
        };
        return InspectMembers(it, ik);

        static bool IsInstanced (Type type)
        {
            // Instanced types are mutable user types that are passed by reference when crossing the
            // interop boundary (as opposed to serialized immutable types, which are copied by value).
            if (!IsUserType(type)) return false;
            if (type.IsInterface) return true;
            return type.IsClass && !IsStatic(type) && !IsRecord(type); // records are immutable by convention
        }
    }

    private T InspectMembers<T> (T surf, InteropKind ik) where T : SurfaceMeta
    {
        var members = (ICollection<MemberMeta>)surf.Members;
        foreach (var evt in surf.Clr.GetEvents())
            members.Add(InspectEvent(evt, ik, surf));
        foreach (var prop in surf.Clr.GetProperties())
            if (ShouldInspectProperty(prop))
                members.Add(InspectProperty(prop, ik, surf));
        foreach (var method in surf.Clr.GetMethods())
            if (ShouldInspectMethod(method))
                members.Add(InspectMethod(method, ik, surf));
        return surf;

        static bool ShouldInspectProperty (PropertyInfo prop)
        {
            if (prop.GetIndexParameters().Length != 0) return false;
            if (prop.DeclaringType!.IsInterface)
                return prop.GetMethod?.IsAbstract == true ||
                       prop.SetMethod?.IsAbstract == true;
            return true;
        }

        static bool ShouldInspectMethod (MethodInfo method)
        {
            if (method.IsSpecialName) return false;
            if (method.DeclaringType!.FullName == typeof(object).FullName) return false;
            if (method.DeclaringType!.IsInterface) return method.IsAbstract;
            return !method.IsStatic;
        }
    }

    private EventMeta InspectEvent (EventInfo evt, InteropKind ik, SurfaceMeta srf) => new(evt) {
        IK = ik,
        Surf = srf,
        Name = evt.Name,
        JSName = WithPref(Pref.Event, evt.Name, BuildJSName(evt.Name)),
        TypeSyntax = BuildSyntax(evt.EventHandlerType!, GetNullity(evt)),
        Args = evt.EventHandlerType!.GetMethod("Invoke")!.GetParameters()
            .Select(p => InspectArg(p, GetNullity(evt, p), ik)).ToArray()
    };

    private PropertyMeta InspectProperty (PropertyInfo prop, InteropKind ik, SurfaceMeta srf) => new(prop) {
        IK = ik,
        Surf = srf,
        Name = prop.Name,
        JSName = WithPref(Pref.Property, prop.Name, BuildJSName(prop.Name)),
        TypeSyntax = BuildSyntax(prop.PropertyType, GetNullity(prop)),
        Get = prop.GetMethod != null ? InspectValue(prop.PropertyType, GetNullity(prop), ik) : null,
        Set = prop.SetMethod != null ? InspectValue(prop.PropertyType, GetNullity(prop), ik.Invert) : null
    };

    private MethodMeta InspectMethod (MethodInfo method, InteropKind ik, SurfaceMeta srf) => new(method) {
        IK = ik,
        Surf = srf,
        Name = method.Name,
        JSName = WithPref(Pref.Method, method.Name, BuildJSName(method.Name)),
        Args = method.GetParameters().Select(p => InspectArg(p, GetNullity(p), ik.Invert)).ToArray(),
        Return = InspectValue(method.ReturnParameter.ParameterType, GetNullity(method.ReturnParameter), ik),
        Void = IsVoid(method.ReturnParameter.ParameterType),
        Async = IsTaskLike(method.ReturnParameter.ParameterType)
    };

    private ArgumentMeta InspectArg (ParameterInfo param, NullabilityInfo nil, InteropKind ik) => new(param) {
        Name = param.Name!,
        JSName = BuildJSName(param.Name!),
        Value = InspectValue(param.ParameterType, nil, ik)
    };

    private ValueMeta InspectValue (Type type, NullabilityInfo nil, InteropKind ik) => new() {
        Type = InspectType(type, ik),
        TypeSyntax = BuildSyntax(type, nil),
        Nullable = IsNullable(type, nil)
    };

    private TypeMeta InspectType (Type type, InteropKind ik)
    {
        for (var clr = type; clr.IsNested && IsUserType(clr.DeclaringType!); clr = clr.DeclaringType!)
            crawled.TryAdd(clr.DeclaringType!, new(clr.DeclaringType!));
        return InspectInstance(type, ik) ?? srd.Inspect(type, ik) ?? new TypeMeta(type);
    }

    private SurfaceProxy BuildProxy (Type type, InteropKind ik)
    {
        var space = "Bootsharp.Generated." + (ik == InteropKind.Export ? "Exports" : "Imports");
        if (type.Namespace != null) space += $".{type.Namespace}";
        var name = "JS" + (type.IsInterface ? type.Name[1..] : type.Name);
        var id = $"{space}.{name}".Replace(".", "_").Replace('+', '_');
        var stx = $"global::{space}.{name}";
        var js = type.Namespace == null ? name : $"{type.Namespace}.{name}".Replace(".", "_");
        return new SurfaceProxy { Id = id, Space = space, Name = name, Syntax = stx, JS = js };
    }

    private InteropKind? ResolveIK (MemberInfo info)
    {
        foreach (var attr in info.CustomAttributes)
            if (ResolveIK(attr) is { } ik)
                return ik;
        return null;
    }

    private InteropKind? ResolveIK (CustomAttributeData attr)
    {
        if (attr.AttributeType.FullName == typeof(ExportAttribute).FullName) return InteropKind.Export;
        if (attr.AttributeType.FullName == typeof(ImportAttribute).FullName) return InteropKind.Import;
        return null;
    }
}
