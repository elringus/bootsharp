using System.Reflection;
using System.Xml.Linq;

namespace Bootsharp.Publish;

internal sealed class SolutionInspector
{
    private readonly TypeInspector types = new();
    private readonly List<DocMeta> docs = [];
    private readonly List<string> warns = [];

    /// <summary>
    /// Inspects specified solution assembly paths in the output directory.
    /// </summary>
    /// <param name="directory">Absolute path to directory containing compiled assemblies.</param>
    /// <param name="paths">Absolute paths of the assemblies to inspect.</param>
    public SolutionInspection Inspect (string directory, IEnumerable<string> paths)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var assemblyPath in paths)
            try { InspectAssembly(assemblyPath, ctx); }
            catch (Exception e) { AddSkippedWarning(assemblyPath, e); }
        return new(ctx) {
            Types = types.Collect(),
            Docs = docs.ToArray(),
            Warnings = warns.ToArray()
        };
    }

    private void InspectAssembly (string assemblyPath, MetadataLoadContext ctx)
    {
        var name = Path.GetFileNameWithoutExtension(assemblyPath);
        if (!IsUserAssembly(name)) return;
        types.Inspect(ctx.LoadFromAssemblyPath(assemblyPath));
        InspectDocs(assemblyPath, name);
    }

    private void AddSkippedWarning (string assemblyPath, Exception exception)
    {
        var fileName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{fileName}' assembly; " +
                      $"affected interop members won't be available in JavaScript. Error: {exception.Message}";
        warns.Add(message);
    }

    private void InspectDocs (string assemblyPath, string assemblyName)
    {
        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (File.Exists(xmlPath)) docs.Add(new(assemblyName, XDocument.Load(xmlPath)));
    }
}
