using System;
using System.Collections.Generic;
using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class LibraryGenerator
{
    private readonly HashSet<string> declaredObjects = new();
    private readonly NamespaceBuilder spaceBuilder;

    public LibraryGenerator (NamespaceBuilder spaceBuilder)
    {
        this.spaceBuilder = spaceBuilder;
    }

    public string GenerateSideLoad (string runtimeJS, string wasmUri,
        string entryAssemblyUri, AssemblyInspector inspector)
    {
        var bootUris = GenerateBootUris(wasmUri, entryAssemblyUri, inspector);
        return new LibraryTemplate {
            RuntimeJS = runtimeJS,
            InitJS = JoinLines(GenerateInitJS(inspector), bootUris)
        }.Build();
    }

    public string GenerateEmbedded (string runtimeJS, byte[] wasmBytes,
        string entryAssemblyName, AssemblyInspector inspector)
    {
        var embedJS = new EmbedTemplate {
            RuntimeWasm = Convert.ToBase64String(wasmBytes),
            Assemblies = inspector.Assemblies,
            EntryAssemblyName = entryAssemblyName
        }.Build();
        return new LibraryTemplate {
            RuntimeJS = runtimeJS,
            InitJS = JoinLines(GenerateInitJS(inspector), embedJS)
        }.Build();
    }

    private string GenerateInitJS (AssemblyInspector inspector)
    {
        return JoinLines(
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Invokable).Select(GenerateInvokableBinding)),
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Function).Select(GenerateFunctionDeclaration)),
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Event).Select(GenerateEventDeclaration)),
            JoinLines(inspector.Types.Where(t => t.IsEnum).Select(GenerateEnumDeclaration))
        );
    }

    private string GenerateBootUris (string wasmUri, string entryAssemblyUri, AssemblyInspector inspector)
    {
        var assemblies = inspector.Assemblies.Select(a => $"\"{a.Name}\",");
        return JoinLines(1,
            "exports.getBootUris = () => ({", JoinLines(2, true,
                $"wasm: \"{wasmUri}\",",
                $"entryAssembly: \"{entryAssemblyUri}\",",
                "assemblies: [", JoinLines(assemblies, 3, true), "]"),
            "});"
        );
    }

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
