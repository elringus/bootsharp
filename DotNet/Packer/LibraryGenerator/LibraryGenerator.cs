using System;
using System.Collections.Generic;
using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class LibraryGenerator
{
    private readonly HashSet<string> declaredObjects = new();
    private readonly NamespaceBuilder spaceBuilder;
    private readonly AssemblyInspector inspector;
    private readonly string runtimeJS;
    private readonly string entryAssemblyName;
    private readonly bool worker;

    public LibraryGenerator (NamespaceBuilder spaceBuilder, AssemblyInspector inspector,
        string runtimeJS, string entryAssemblyName, bool worker)
    {
        this.spaceBuilder = spaceBuilder;
        this.inspector = inspector;
        this.runtimeJS = runtimeJS;
        this.entryAssemblyName = entryAssemblyName;
        this.worker = worker;
    }

    public string GenerateSideLoad (string wasmUri) =>
        GenerateLibrary(new SideLoadTemplate {
            WasmUri = wasmUri,
            Assemblies = inspector.Assemblies,
            EntryAssemblyUri = entryAssemblyName
        }.Build());

    public string GenerateEmbedded (byte[] wasmBytes) =>
        GenerateLibrary(new EmbedTemplate {
            RuntimeWasm = Convert.ToBase64String(wasmBytes),
            Assemblies = inspector.Assemblies,
            EntryAssemblyName = entryAssemblyName
        }.Build());

    private string GenerateLibrary (string initJS) =>
        new LibraryTemplate {
            RuntimeJS = runtimeJS,
            InitJS = JoinLines(GenerateBindings(), initJS),
            Worker = worker
        }.Build();

    private string GenerateBindings () => JoinLines(
        JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Invokable).Select(GenerateInvokableBinding)),
        JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Function).Select(GenerateFunctionDeclaration)),
        JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Event).Select(GenerateEventDeclaration)),
        JoinLines(inspector.Types.Where(t => t.IsEnum).Select(GenerateEnumDeclaration))
    );

    private string GenerateInvokableBinding (Method method)
    {
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var methodArgs = $"'{method.Assembly}', '{method.Name}'" + (funcArgs == "" ? "" : $", {funcArgs}");
        var invoke = method.Async ? "invokeAsync" : "invoke";
        var body = $"exports.{invoke}({methodArgs})";
        var js = $"exports.{method.Namespace}.{method.Name} = ({funcArgs}) => {body};";
        return EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string GenerateFunctionDeclaration (Method method)
    {
        var js = $"exports.{method.Namespace}.{method.Name} = undefined;";
        return EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string GenerateEventDeclaration (Method method)
    {
        var js = $"exports.{method.Namespace}.{method.Name} = new exports.Event();";
        return EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string GenerateEnumDeclaration (Type @enum)
    {
        var values = Enum.GetNames(@enum);
        var fields = string.Join(", ", values.Select(v => $"{v}: \"{v}\""));
        var space = spaceBuilder.Build(@enum);
        var js = $"exports.{space}.{@enum.Name} = {{ {fields} }};";
        return EnsureNamespaceObjectsDeclared(space, js);
    }

    private string EnsureNamespaceObjectsDeclared (string space, string js)
    {
        var objects = BuildObjectNamesForNamespace(space);
        foreach (var obj in objects)
            if (declaredObjects.Add(obj))
                js = JoinLines($"exports.{obj} = {{}};", js);
        return js;
    }

    private List<string> BuildObjectNamesForNamespace (string space)
    {
        var parts = space.Split('.');
        var names = new List<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            var previousParts = i > 0 ? string.Join(".", parts.Take(i)) + "." : "";
            names.Add(previousParts + parts[i]);
        }
        names.Reverse();
        return names;
    }
}
