using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bootsharp.Publish;

internal static class SpecializationResolver
{
    private static AsyncLocal<Dictionary<Type, Specialization>> async { get; } = new();
    private static Dictionary<Type, Specialization> map => async.Value ??= [];

    public static void Resolve (Assembly ass)
    {
        var imports = new Dictionary<Type, (Type Type, string? JS, string? Decl)>();
        var exports = new List<(Type Clr, Type Type)>();
        foreach (var type in ass.GetExportedTypes())
        foreach (var attr in type.CustomAttributes)
            if (IsAttribute<SpecializeImportAttribute>(attr))
                imports[GetAttributeArg<Type>(attr)!] =
                    (type, GetAttributeArg<string>(attr, 1), GetAttributeArg<string>(attr, 2));
            else if (IsAttribute<SpecializeExportAttribute>(attr))
                exports.Add((GetAttributeArg<Type>(attr)!, type));
        foreach (var (clr, export) in exports)
            map[clr] = imports.TryGetValue(clr, out var import)
                ? new() { Import = import.Type, Export = export, JS = import.JS, Decl = import.Decl }
                : throw new Error($"Specialized export '{export.FullName}' is missing the paired import.");
    }

    public static bool IsSpecialized (Type type) => IsSpecialized(type, out _);
    public static bool IsSpecialized (Type type, [NotNullWhen(true)] out Specialization? sp)
    {
        var key = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return (sp = map.GetValueOrDefault(key)) != null;
    }
}
