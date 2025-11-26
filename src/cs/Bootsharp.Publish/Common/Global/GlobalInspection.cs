global using static Bootsharp.Publish.GlobalInspection;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal static class GlobalInspection
{
    public static MetadataLoadContext CreateLoadContext (string directory)
    {
        var assemblyPaths = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll").ToList();
        foreach (var path in Directory.GetFiles(directory, "*.dll"))
            if (assemblyPaths.All(p => Path.GetFileName(p) != Path.GetFileName(path)))
                assemblyPaths.Add(path);
        var resolver = new PathAssemblyResolver(assemblyPaths);
        return new MetadataLoadContext(resolver);
    }

    public static bool ShouldIgnoreAssembly (string filePath)
    {
        var assemblyName = Path.GetFileName(filePath);
        return assemblyName.StartsWith("System.") ||
               assemblyName.StartsWith("Microsoft.") ||
               assemblyName.StartsWith("netstandard") ||
               assemblyName.StartsWith("mscorlib");
    }

    public static string WithPrefs (IReadOnlyCollection<Preference> prefs, string input, string @default)
    {
        foreach (var pref in prefs)
            if (Regex.IsMatch(input, pref.Pattern))
                return Regex.Replace(input, pref.Pattern, pref.Replacement);
        return @default;
    }
}
