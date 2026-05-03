global using static Bootsharp.Publish.GlobalInspection;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal static class GlobalInspection
{
    public static MetadataLoadContext CreateLoadContext (string directory)
    {
        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var assemblyPaths = Directory.GetFiles(runtimeDir, "*.dll").Order().ToList();
        foreach (var path in Directory.GetFiles(directory, "*.dll").Order())
            if (assemblyPaths.All(p => Path.GetFileName(p) != Path.GetFileName(path)))
                assemblyPaths.Add(path);
        var resolver = new PathAssemblyResolver(assemblyPaths);
        return new MetadataLoadContext(resolver);
    }

    public static bool IsUserAssembly (string assemblyName) =>
        !assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
        !assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) &&
        !assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
        !assemblyName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase);

    public static bool IsUserType (Type type)
    {
        if (type.IsArray) return false;
        return IsUserAssembly(type.Assembly.FullName!);
    }

    public static bool IsInstancedType (Type type)
    {
        // Instanced types are mutable user types that are passed by reference when crossing the
        // interop boundary (as opposed to serialized immutable types, which are copied by value).
        if (!IsUserType(type)) return false;
        if (type.IsInterface) return true;
        var isRecord = type.GetMethod("<Clone>$", // records are immutable by convention
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) != null;
        var isStatic = type.IsAbstract && type.IsSealed;
        return type.IsClass && !isStatic && !isRecord;
    }

    public static bool IsAutoProperty (PropertyInfo prop)
    {
        var backingFieldName = $"<{prop.Name}>k__BackingField";
        var backingField = prop.DeclaringType!.GetField(backingFieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        return backingField != null;
    }

    public static string WithPrefs (IReadOnlyCollection<Preference> prefs, string input, string @default)
    {
        foreach (var pref in prefs)
            if (Regex.IsMatch(input, pref.Pattern))
                return Regex.Replace(input, pref.Pattern, pref.Replacement);
        return @default;
    }
}
