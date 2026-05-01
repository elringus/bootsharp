using System.Reflection;
using System.Xml.Linq;

namespace Bootsharp.Publish;

internal sealed class SolutionInspector
{
    private readonly List<InterfaceMeta> staticInterfaces = [];
    private readonly List<InterfaceMeta> instancedInterfaces = [];
    private readonly List<MemberMeta> staticMembers = [];
    private readonly List<DocumentationMeta> docs = [];
    private readonly List<string> warnings = [];
    private readonly TypeInspector typeInspector = new();
    private readonly SerializedInspector serdeInspector = new();
    private readonly MemberInspector memberInspector;
    private readonly InterfaceInspector interfaceInspector;

    public SolutionInspector (Preferences prefs, string entryAssemblyName)
    {
        memberInspector = new(prefs, typeInspector, serdeInspector);
        interfaceInspector = new(memberInspector, entryAssemblyName);
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
        StaticInterfaces = staticInterfaces.DistinctBy(i => i.FullName).ToArray(),
        InstancedInterfaces = instancedInterfaces.DistinctBy(i => i.FullName).ToArray(),
        StaticMembers = staticMembers.ToArray(),
        Types = typeInspector.Collect(),
        Serialized = serdeInspector.Collect(),
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
        foreach (var exported in assembly.GetExportedTypes())
            InspectExportedType(exported);
        foreach (var attribute in assembly.CustomAttributes)
            InspectAssemblyAttribute(attribute);
    }

    private void InspectExportedType (Type type)
    {
        if (type.Namespace?.StartsWith("Bootsharp.Generated") ?? false) return;
        foreach (var evt in type.GetEvents(BindingFlags.Public | BindingFlags.Static))
            InspectStaticEvent(evt);
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            InspectStaticMethod(method);
    }

    private void InspectAssemblyAttribute (CustomAttributeData attr)
    {
        var interop = default(InteropKind);
        var name = attr.AttributeType.FullName;
        if (name == typeof(ExportAttribute).FullName) interop = InteropKind.Export;
        else if (name == typeof(ImportAttribute).FullName) interop = InteropKind.Import;
        else return;
        foreach (var arg in (IEnumerable<CustomAttributeTypedArgument>)attr.ConstructorArguments[0].Value!)
            InspectStaticInterface((Type)arg.Value!, interop);
    }

    private void InspectStaticMethod (MethodInfo info)
    {
        var interop = default(InteropKind?);
        foreach (var attr in info.CustomAttributes.Select(a => a.AttributeType.FullName))
            if (attr == typeof(ExportAttribute).FullName) interop = InteropKind.Export;
            else if (attr == typeof(ImportAttribute).FullName) interop = InteropKind.Import;
        if (interop is { } ik)
        {
            var method = memberInspector.Inspect(info, ik);
            staticMembers.Add(method);
            InspectMember(method);
        }
    }

    private void InspectStaticEvent (EventInfo info)
    {
        var interop = default(InteropKind?);
        foreach (var attr in info.CustomAttributes.Select(a => a.AttributeType.FullName))
            if (attr == typeof(ExportAttribute).FullName) interop = InteropKind.Export;
            else if (attr == typeof(ImportAttribute).FullName) interop = InteropKind.Import;
        if (interop is { } ik)
        {
            var evt = memberInspector.Inspect(info, ik);
            staticMembers.Add(evt);
            InspectMember(evt);
        }
    }

    private void InspectStaticInterface (Type type, InteropKind interop)
    {
        var interfaceMeta = interfaceInspector.Inspect(type, interop);
        staticInterfaces.Add(interfaceMeta);
        foreach (var member in interfaceMeta.Members)
            InspectMember(member);
    }

    private void InspectMember (MemberMeta meta)
    {
        // When interop instance is an argument of exported method, it's imported (JS) API and vice versa.
        var interop = meta.Interop == InteropKind.Export ? InteropKind.Import : InteropKind.Export;
        if (meta is PropertyMeta prop)
        {
            if (prop.CanSet) InspectType(prop.Value.Type.Clr, interop);
            if (prop.CanGet) InspectType(prop.Value.Type.Clr, prop.Interop);
        }
        else if (meta is MethodMeta method)
        {
            foreach (var arg in method.Arguments)
                InspectType(arg.Value.Type.Clr, interop);
            if (!method.Void) InspectType(method.Value.Type.Clr, method.Interop);
        }
        else if (meta is EventMeta evt)
            foreach (var arg in evt.Arguments)
                InspectType(arg.Value.Type.Clr, evt.Interop);
    }

    private void InspectType (Type type, InteropKind interop)
    {
        if (IsInstancedInterface(type, out var instanceType))
            instancedInterfaces.Add(interfaceInspector.Inspect(instanceType, interop));
    }
}
