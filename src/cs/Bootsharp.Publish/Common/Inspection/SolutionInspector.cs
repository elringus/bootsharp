using System.Xml.Linq;
using Microsoft.Build.Utilities;

namespace Bootsharp.Publish;

internal sealed class SolutionInspector (TaskLoggingHelper logger)
{
    private readonly List<InspectedAssembly> asses = [];
    private readonly TypeInspector types = new();
    private readonly List<DocMeta> docs = [];

    /// <summary>
    /// Inspects specified solution assembly paths in the output directory.
    /// </summary>
    /// <param name="paths">Absolute paths of the assemblies to inspect.</param>
    public SolutionInspection Inspect (IEnumerable<string> paths)
    {
        var ctx = new InspectionContext();
        foreach (var pth in paths) LoadAssembly(pth, ctx);
        foreach (var ass in asses) ResolvePreferences(ass);
        foreach (var ass in asses) types.Inspect(ass.Assembly);
        foreach (var ass in asses) InspectDocs(ass);
        return new(ctx) { Types = types.Collect(), Docs = docs.ToArray() };
    }

    private void LoadAssembly (string path, InspectionContext ctx)
    {
        if (!IsUserAssembly(Path.GetFileNameWithoutExtension(path))) return;
        try { asses.Add(ctx.Load(path)); }
        catch (Exception e) { Warn(path, e); }
    }

    private void ResolvePreferences (InspectedAssembly ass)
    {
        try { Preferences.Resolve(ass.Assembly); }
        catch (Exception e) { Warn(ass.Path, e); }
    }

    private void InspectDocs (InspectedAssembly ass)
    {
        var xmlPath = Path.ChangeExtension(ass.Path, ".xml");
        var name = Path.GetFileNameWithoutExtension(ass.Name);
        if (File.Exists(xmlPath)) docs.Add(new(name, XDocument.Load(xmlPath)));
    }

    private void Warn (string path, Exception ex) => logger.LogWarning(
        $"Failed to inspect '{Path.GetFileName(path)}' assembly. Error: {ex}");
}
