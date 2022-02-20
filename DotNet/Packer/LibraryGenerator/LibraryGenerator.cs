using System.Collections.Generic;
using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class LibraryGenerator
{
    public string GenerateSideLoad (string runtimeJS, AssemblyInspector inspector)
    {
        return new LibraryTemplate {
            InitJS = GenerateInitJS(inspector.Methods),
            RuntimeJS = runtimeJS
        }.Build();
    }

    public string GenerateEmbedded (string runtimeJS, string runtimeWasm,
        string entryAssemblyName, AssemblyInspector inspector)
    {
        var embedJS = new EmbedTemplate {
            RuntimeWasm = runtimeWasm,
            Assemblies = inspector.Assemblies,
            EntryAssemblyName = entryAssemblyName
        }.Build();
        return new LibraryTemplate {
            InitJS = GenerateInitJS(inspector.Methods),
            RuntimeJS = runtimeJS
        }.Build(embedJS);
    }

    private string GenerateInitJS (IReadOnlyCollection<Method> methods)
    {
        var invokable = methods.Where(m => m.Type == MethodType.Invokable);
        var functions = methods.Where(m => m.Type == MethodType.Function);
        return JoinLines(
            JoinLines(invokable.Select(GenerateInvokableBinding)),
            JoinLines(functions.Select(GenerateFunctionDeclaration))
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

    private string EnsureNamespaceObjectsDeclared (string space, string js)
    {
        var declared = new HashSet<string>();
        var objects = BuildObjectNamesForNamespace(space);
        foreach (var obj in objects)
            if (declared.Add(obj))
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
