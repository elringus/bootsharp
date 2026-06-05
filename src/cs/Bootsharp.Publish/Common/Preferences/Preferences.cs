using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp.Publish;

internal static class Preferences
{
    private sealed class Prefs
    {
        public Dictionary<Type, Specialization> Specs { get; } = [];
        public Func<Type, string, string?>? Module, Node;
        public Func<MemberInfo, string, string?>? Member;
    }

    private static AsyncLocal<Prefs> async { get; } = new();
    private static Prefs prefs => async.Value ??= new();

    private static readonly FieldInfo moduleField = GetMetaField(nameof(TypeMeta.JSModule));
    private static readonly FieldInfo nodeField = GetMetaField(nameof(TypeMeta.JSNode));

    public static void Resolve (Assembly ass)
    {
        ResolveRenames(ass);
        ResolveSpecs(ass);
    }

    public static IReadOnlyCollection<TypeMeta> Rename (IReadOnlyCollection<TypeMeta> types)
    {
        if (prefs is { Module: null, Node: null, Member: null }) return types;
        var renamed = new List<TypeMeta>(types.Count);
        foreach (var type in types)
        {
            if (prefs.Node is { } renameNode)
                if (renameNode(type.Clr, type.JSNode) is { Length: > 0 } node) nodeField.SetValue(type, node);
                else continue;
            if (prefs.Module is { } renameModule)
                if (renameModule(type.Clr, type.JSModule) is { Length: > 0 } md) moduleField.SetValue(type, md);
                else moduleField.SetValue(type, "index");
            if (prefs.Member is { } renameMember && type is SurfaceMeta { MemberList: { Count: > 0 } mm })
                for (var i = mm.Count - 1; i >= 0; i--)
                    if (renameMember(mm[i].Info, mm[i].JSName) is not { Length: > 0 } name) mm.RemoveAt(i);
                    else mm[i] = mm[i] with { JSName = name };
            renamed.Add(type);
        }
        return renamed;
    }

    public static bool IsSpecialized (Type type) => IsSpecialized(type, out _);
    public static bool IsSpecialized (Type type, [NotNullWhen(true)] out Specialization? sp)
    {
        var key = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return (sp = prefs.Specs.GetValueOrDefault(key)) != null;
    }

    private static void ResolveRenames (Assembly ass)
    {
        foreach (var type in ass.GetExportedTypes())
        foreach (var meth in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        foreach (var attr in meth.CustomAttributes)
            if (IsAttribute<RenameModuleAttribute>(attr))
                prefs.Module = meth.CreateDelegate<Func<Type, string, string?>>();
            else if (IsAttribute<RenameNodeAttribute>(attr))
                prefs.Node = meth.CreateDelegate<Func<Type, string, string?>>();
            else if (IsAttribute<RenameMemberAttribute>(attr))
                prefs.Member = meth.CreateDelegate<Func<MemberInfo, string, string?>>();
    }

    private static void ResolveSpecs (Assembly ass)
    {
        var imports = new Dictionary<Type, (Type Type, string? CS, string? JS, string? JSCtor, string? Decl)>();
        var exports = new List<(Type Clr, Type Type)>();
        foreach (var type in ass.GetExportedTypes())
        foreach (var attr in type.CustomAttributes)
            if (IsAttribute<SpecializeImportAttribute>(attr))
                imports[GetAttributeArg<Type>(attr)!] = (type,
                    GetAttributeArg<string>(attr, 1), GetAttributeArg<string>(attr, 2),
                    GetAttributeArg<string>(attr, 3), GetAttributeArg<string>(attr, 4));
            else if (IsAttribute<SpecializeExportAttribute>(attr))
                exports.Add((GetAttributeArg<Type>(attr)!, type));
        foreach (var (clr, export) in exports)
            prefs.Specs[clr] = imports.TryGetValue(clr, out var i)
                ? new() { Import = i.Type, Export = export, CS = i.CS, JS = i.JS, JSCtor = i.JSCtor, Decl = i.Decl }
                : throw new Error($"Specialized export '{export.FullName}' is missing the paired import.");
    }

    private static FieldInfo GetMetaField (string prop) =>
        typeof(TypeMeta).GetField($"<{prop}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;

    extension (SurfaceMeta srf)
    {
        private IList<MemberMeta> MemberList => (IList<MemberMeta>)srf.Members;
    }
}
