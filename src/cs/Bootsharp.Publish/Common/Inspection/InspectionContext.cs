using System.Runtime.Loader;

namespace Bootsharp.Publish;

internal sealed class InspectionContext () : AssemblyLoadContext("Bootsharp", isCollectible: true)
{
    public InspectedAssembly Load (string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryStream(bytes);
        var ass = LoadFromStream(stream);
        var name = Path.GetFileName(path);
        return new(ass, path, name);
    }
}
