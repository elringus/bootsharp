using System.Reflection;
using System.Xml.Linq;

namespace Bootsharp.Publish;

internal sealed class SolutionInspector
{
    private readonly List<MemberMeta> statics = [];
    private readonly List<InstancedMeta> modules = [];
    private readonly List<DocumentationMeta> docs = [];
    private readonly List<string> warnings = [];
    private readonly SerializedInspector serde;
    private readonly InstancedInspector itd;
    private readonly MemberInspector members;

    public SolutionInspector (Preferences prefs)
    {
        members = new(prefs, (type, ik) => itd!.Inspect(type, ik) ?? serde!.Inspect(type, ik) ?? new TypeMeta(type));
        itd = new(members);
        serde = new(itd);
    }

    /// <summary>
    /// Inspects specified solution assembly paths in the output directory.
    /// </summary>
    /// <param name="directory">Absolute path to directory containing compiled assemblies.</param>
    /// <param name="paths">Absolute paths of the assemblies to inspect.</param>
    public SolutionInspection Inspect (string directory, IEnumerable<string> paths)
    {
        var ctx = CreateLoadContext(directory);
        foreach (var assemblyPath in paths)
            try { InspectAssemblyFile(assemblyPath, ctx); }
            catch (Exception e) { AddSkippedAssemblyWarning(assemblyPath, e); }
        return CreateInspection(ctx);
    }

    private void InspectAssemblyFile (string assemblyPath, MetadataLoadContext ctx)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        if (!IsUserAssembly(assemblyName)) return;
        InspectDocumentation(assemblyPath, assemblyName);
        InspectAssembly(ctx.LoadFromAssemblyPath(assemblyPath));
    }

    private void AddSkippedAssemblyWarning (string assemblyPath, Exception exception)
    {
        var fileName = Path.GetFileName(assemblyPath);
        var message = $"Failed to inspect '{fileName}' assembly; " +
                      $"affected interop members won't be available in JavaScript. Error: {exception.Message}";
        warnings.Add(message);
    }

    private SolutionInspection CreateInspection (MetadataLoadContext ctx) => new(ctx) {
        Static = statics.ToArray(),
        Modules = modules.ToArray(),
        Instanced = itd.Collect().Except(modules).ToArray(),
        Serialized = serde.Collect(),
        Documentation = docs.ToArray(),
        Warnings = warnings.ToArray()
    };

    private void InspectDocumentation (string assemblyPath, string assemblyName)
    {
        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (File.Exists(xmlPath)) docs.Add(new(assemblyName, XDocument.Load(xmlPath)));
    }

    private void InspectAssembly (Assembly assembly)
    {
        foreach (var type in assembly.GetExportedTypes())
            InspectStatic(type);
        foreach (var attr in assembly.CustomAttributes)
            InspectModules(attr);
    }

    private void InspectStatic (Type type)
    {
        if (type.Namespace?.StartsWith("Bootsharp.Generated") ?? false) return;
        foreach (var evt in type.GetEvents(BindingFlags.Public | BindingFlags.Static))
            if (ResolveInterop(evt) is { } ik)
                statics.Add(members.Inspect(evt, ik, null));
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            if (ResolveInterop(method) is { } ik)
                statics.Add(members.Inspect(method, ik, null));
    }

    private void InspectModules (CustomAttributeData attr)
    {
        if (ResolveInterop(attr) is not { } ik) return;
        foreach (var arg in (IEnumerable<CustomAttributeTypedArgument>)attr.ConstructorArguments[0].Value!)
            if (itd.Inspect((Type)arg.Value!, ik) is { } it)
                if (ik == InteropKind.Export || it.Clr.IsInterface)
                    modules.Add(it);
    }

    private InteropKind? ResolveInterop (MemberInfo info)
    {
        foreach (var attr in info.CustomAttributes)
            if (ResolveInterop(attr) is { } ik)
                return ik;
        return null;
    }

    private InteropKind? ResolveInterop (CustomAttributeData attr)
    {
        if (attr.AttributeType.FullName == typeof(ExportAttribute).FullName) return InteropKind.Export;
        if (attr.AttributeType.FullName == typeof(ImportAttribute).FullName) return InteropKind.Import;
        return null;
    }
}
