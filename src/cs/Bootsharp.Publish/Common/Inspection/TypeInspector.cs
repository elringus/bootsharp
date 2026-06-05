using System.Reflection;
using System.Runtime.Loader;

namespace Bootsharp.Publish;

internal sealed class TypeInspector
{
    internal delegate InstanceMeta? InspectInstanced (Type type, InteropKind ik, Nullity? nul);

    private readonly Dictionary<(string Syntax, InteropKind IK), InstanceMeta> its = [];
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
        TypeMeta[] types = [..surfaces, ..its.Values, ..srd.Collect()];
        GenericDisambiguator.Disambiguate(types);
        OverloadDisambiguator.Disambiguate(types);
        var clrs = types.Select(t => t.Clr).ToHashSet();
        return Preferences.Rename([..types, ..crawled.Values.Where(c => !clrs.Contains(c.Clr))]);
    }

    private StaticMeta? InspectStatic (Type type)
    {
        if (!IsUserType(type)) return null;
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
                members.Add(InspectMethod(method, ik, st, null));
        return members.Count > 0 ? st : null;
    }

    private ModuleMeta? InspectModule (Type type, InteropKind ik)
    {
        if (!inspectedModuleTypes.Add(type) || IsStatic(type) ||
            (ik == InteropKind.Import && !type.IsInterface)) return null;
        var proxy = BuildProxy(type, BuildId(type), ik);
        var md = new ModuleMeta(type) { IK = ik, Proxy = proxy, Members = new List<MemberMeta>() };
        return InspectMembers(md, ik, null);
    }

    private InstanceMeta? InspectInstance (Type type, InteropKind ik, Nullity? nul)
    {
        var id = BuildId(type, type.IsGenericType ? nul : null); // nullity only matter for generic args
        var key = (id, ik);
        if (its.TryGetValue(key, out var it)) return it;
        if (IsTaskWithResult(type, out var result)) return InspectInstance(result, ik, nul!.GenericTypeArguments[0]);
        if (!IsInstanced(type)) return null;
        if (IsDelegate(type)) return its[key] = InspectDelegate(type, ik, nul);
        if (!Preferences.IsSpecialized(type, out var sp) && ik == InteropKind.Import && !type.IsInterface)
            return InspectInstance(type, InteropKind.Export, nul)!; // likely passing back an exported instance
        return InspectMembers(its[key] = new(type) {
            IK = ik,
            Proxy = BuildProxy(type, id, ik, sp),
            Members = new List<MemberMeta>(),
            Exporter = ResolveExporter(),
            Importer = ResolveImporter(),
        }, ik, nul);

        static bool IsInstanced (Type type)
        {
            // Instanced types are mutable user types that are passed by reference when crossing the
            // interop boundary (as opposed to serialized immutable types, which are copied by value).
            if (!IsUserType(type) || type.ContainsGenericParameters) return false;
            if (type.IsInterface || Preferences.IsSpecialized(type)) return true;
            return type.IsClass && !IsStatic(type) && !IsRecord(type); // records are immutable by convention
        }

        string? ResolveExporter ()
        {
            if (ik != InteropKind.Export) return null;
            if (sp != null || type.GetEvents().Length > 0) return "Export";
            return null;
        }

        string? ResolveImporter ()
        {
            if (ik != InteropKind.Import) return null;
            if ((sp?.For(type, ik) ?? type).GetEvents().Length > 0) return $"import_{id}";
            return null;
        }
    }

    private DelegateMeta InspectDelegate (Type type, InteropKind ik, Nullity? nul)
    {
        var members = new List<MemberMeta>();
        var proxy = BuildProxy(type, BuildId(type), ik);
        var del = new DelegateMeta(type) { IK = ik, Proxy = proxy, Members = members };
        members.Add(InspectMethod(type.GetMethod("Invoke")!, ik, del, nul));
        return del;
    }

    private T InspectMembers<T> (T surf, InteropKind ik, Nullity? nul) where T : ProxyMeta
    {
        var members = (ICollection<MemberMeta>)surf.Members;
        var sp = surf.Proxy as SpecializedProxy;
        var clr = sp?.Import.Clr ?? surf.Clr;
        foreach (var evt in clr.GetEvents())
            members.Add(InspectEvent(evt, ik, surf));
        foreach (var prop in clr.GetProperties())
            if (ShouldInspectProperty(prop))
                members.Add(InspectProperty(prop, ik, surf));
        foreach (var method in clr.GetMethods())
            if (ShouldInspectMethod(method))
                members.Add(InspectMethod(method, ik, surf, nul));
        return surf;

        bool ShouldInspectProperty (PropertyInfo prop)
        {
            if (prop.GetIndexParameters().Length != 0) return false;
            if (sp != null || prop.DeclaringType!.IsInterface)
                return prop.GetMethod?.IsAbstract == true ||
                       prop.SetMethod?.IsAbstract == true;
            return true;
        }

        bool ShouldInspectMethod (MethodInfo method)
        {
            if (method.IsSpecialName) return false;
            if (method.DeclaringType!.FullName == typeof(object).FullName) return false;
            if (sp != null || method.DeclaringType!.IsInterface) return method.IsAbstract;
            return !method.IsStatic;
        }
    }

    private EventMeta InspectEvent (EventInfo evt, InteropKind ik, SurfaceMeta srf) => new(evt) {
        IK = ik,
        Surf = srf,
        Name = BuildCSName(evt.Name),
        JSName = BuildJSName(evt.Name),
        TypeSyntax = BuildSyntax(evt.EventHandlerType!, GetNullity(evt)),
        Args = evt.EventHandlerType!.GetMethod("Invoke")!.GetParameters()
            .Select(p => InspectArg(p, GetNullity(evt, p), ik)).ToArray()
    };

    private PropertyMeta InspectProperty (PropertyInfo prop, InteropKind ik, SurfaceMeta srf) => new(prop) {
        IK = ik,
        Surf = srf,
        Name = BuildCSName(prop.Name),
        JSName = BuildJSName(prop.Name),
        TypeSyntax = BuildSyntax(prop.PropertyType, GetNullity(prop)),
        Get = prop.GetMethod != null ? InspectValue(prop.PropertyType, GetNullity(prop), ik) : null,
        Set = prop.SetMethod != null ? InspectValue(prop.PropertyType, GetNullity(prop), ik.Invert) : null
    };

    private MethodMeta InspectMethod (MethodInfo meth, InteropKind ik, SurfaceMeta srf, Nullity? nul) => new(meth) {
        IK = ik,
        Surf = srf,
        Name = BuildCSName(meth.Name),
        Endpoint = BuildCSName(meth.Name),
        JSName = BuildJSName(meth.Name),
        Args = meth.GetParameters().Select(p => InspectArg(p, GetNullity(p, nul), ik.Invert)).ToArray(),
        Return = InspectValue(meth.ReturnParameter.ParameterType, GetNullity(meth.ReturnParameter, nul), ik),
        Void = IsVoid(meth.ReturnParameter.ParameterType),
        Async = IsTaskLike(meth.ReturnParameter.ParameterType)
    };

    private ArgumentMeta InspectArg (ParameterInfo param, Nullity nul, InteropKind ik) => new(param) {
        Name = BuildCSName(param.Name!),
        JSName = BuildJSName(param.Name!),
        Value = InspectValue(param.ParameterType, nul, ik)
    };

    private ValueMeta InspectValue (Type type, Nullity nul, InteropKind ik) => new() {
        Type = InspectType(type, ik, nul),
        TypeSyntax = BuildSyntax(type, nul),
        Nullable = IsNullable(type, nul)
    };

    private TypeMeta InspectType (Type type, InteropKind ik, Nullity? nul)
    {
        CrawlInspected(type, ik, nul);
        return InspectInstance(type, ik, nul) ?? srd.Inspect(type, ik, nul) ?? new TypeMeta(type);
    }

    private SurfaceProxy BuildProxy (Type type, string typeId, InteropKind ik, Specialization? sp = null)
    {
        var id = "JS_" + (ik == InteropKind.Export ? "Export_" : "Import_") + typeId;
        var stx = $"global::Bootsharp.Generated.{id}";
        if (sp == null) return new() { Id = id, Syntax = stx };
        return new SpecializedProxy {
            Id = id,
            Syntax = stx,
            Import = new(sp.For(type, InteropKind.Import)),
            Export = new(sp.For(type, InteropKind.Export)),
            CS = sp.CS,
            JS = sp.JS,
            JSCtor = sp.JSCtor,
            Decl = sp.Decl
        };
    }

    private void CrawlInspected (Type type, InteropKind ik, Nullity? nul)
    {
        for (var clr = type; clr.IsNested && IsUserType(clr.DeclaringType!); clr = clr.DeclaringType!)
            crawled.TryAdd(clr.DeclaringType!, new(clr.DeclaringType!));
        if (type.IsGenericMethodParameter)
            foreach (var compatible in FindCompatible(type))
                InspectType(compatible, ik, nul);

        static IEnumerable<Type> FindCompatible (Type param)
        {
            foreach (var ct in param.GetGenericParameterConstraints().Where(IsUserType))
            foreach (var ass in AssemblyLoadContext.GetLoadContext(ct.Assembly)!.Assemblies)
            foreach (var clr in ass.GetExportedTypes())
                if (IsUserType(clr) && !clr.IsAbstract && !clr.ContainsGenericParameters && ct.IsAssignableFrom(clr))
                    yield return clr;
        }
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
