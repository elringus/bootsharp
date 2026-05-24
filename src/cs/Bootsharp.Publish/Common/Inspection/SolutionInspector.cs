using System.Reflection;
using System.Xml.Linq;
using Microsoft.Build.Utilities;

namespace Bootsharp.Publish;

internal sealed class SolutionInspector (string entryAssemblyName, TaskLoggingHelper logger)
{
    private readonly List<Assembly> asses = [];
    private readonly TypeInspector types = new();
    private readonly List<DocMeta> docs = [];

    /// <summary>
    /// Inspects specified solution assembly paths in the output directory.
    /// </summary>
    /// <param name="directory">Absolute path to directory containing compiled assemblies.</param>
    /// <param name="paths">Absolute paths of the assemblies to inspect.</param>
    public SolutionInspection Inspect (string directory, IEnumerable<string> paths)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var pth in paths) LoadAssembly(pth, ctx);
        foreach (var ass in asses) ResolvePreferences(ass);
        foreach (var ass in asses) types.Inspect(ass);
        foreach (var ass in asses) InspectDocs(ass);
        return new(ctx) { Types = types.Collect(), Docs = docs.ToArray() };
    }

    private void LoadAssembly (string path, MetadataLoadContext ctx)
    {
        if (!IsUserAssembly(Path.GetFileNameWithoutExtension(path))) return;
        try { asses.Add(ctx.LoadFromAssemblyPath(path)); }
        catch (Exception e) { Warn(path, e); }
    }

    private void ResolvePreferences (Assembly ass)
    {
        if (ass.GetName().Name == Path.GetFileNameWithoutExtension(entryAssemblyName))
            PreferencesResolver.Resolve(ass);
        try { SpecializationResolver.Resolve(ass); }
        catch (Exception e) { Warn(ass.Location, e); }
    }

    private void InspectDocs (Assembly ass)
    {
        var xmlPath = Path.ChangeExtension(ass.Location, ".xml");
        var name = Path.GetFileNameWithoutExtension(ass.Location);
        if (File.Exists(xmlPath)) docs.Add(new(name, XDocument.Load(xmlPath)));
    }

    private void Warn (string path, Exception ex) => logger.LogWarning(
        $"Failed to inspect '{Path.GetFileName(path)}' assembly. Error: {ex}");
}
