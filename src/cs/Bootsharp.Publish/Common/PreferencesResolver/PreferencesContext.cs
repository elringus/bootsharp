using System.Reflection;
using System.Runtime.Loader;

namespace Bootsharp.Publish;

// https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability
internal sealed class PreferencesContext (string outDir) : AssemblyLoadContext
{
    public Assembly LoadAssembly (string assemblyFileName)
    {
        using var stream = StreamFile(assemblyFileName);
        return LoadFromStream(stream);
    }

    protected override Assembly Load (AssemblyName name)
    {
        return LoadAssembly($"{name.Name}.dll");
    }

    private Stream StreamFile (string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(outDir, fileName));
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}
