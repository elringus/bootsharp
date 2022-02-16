using System.Collections.Generic;
using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class LibraryGenerator
{
    private const string exports = "exports";
    private const string moduleTemplate = @"%RUNTIME_JS%
(function (root, factory) {
    if (typeof exports === 'object' && typeof exports.nodeName !== 'string')
        factory(module.exports, global);
    else factory(root.dotnet, root);
}(typeof self !== 'undefined' ? self : this, function (exports, global) {
    %INIT_JS%
    const bootWithData = exports.boot;
    exports.boot = async function () {
        const bootData = {
            wasm: '%WASM%',
            assemblies: [%DLLS%],
            entryAssemblyName: '%ENTRY%'
        };
        await bootWithData(bootData);
    };
    global.dotnet = exports;
}));";

    private readonly HashSet<string> declaredObjects = new();

    public string Generate (string runtimeJS, string runtimeWasm, string entryName, AssemblyInspector inspector)
    {
        declaredObjects.Clear();
        var initJS = GenerateInitJS(inspector.Methods);
        var dlls = string.Join(", ", inspector.Assemblies.Select(GenerateAssembly));
        return moduleTemplate
            .Replace("%RUNTIME_JS%", runtimeJS)
            .Replace("%ENTRY%", entryName)
            .Replace("%WASM%", runtimeWasm)
            .Replace("%DLLS%", dlls)
            .Replace("%INIT_JS%", initJS);
    }

    private string GenerateAssembly (Assembly assembly)
    {
        return $"{{ name: '{assembly.Name}', data: '{assembly.Base64}' }}";
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
        var funcArgs = BuildFuncArgs();
        var invoke = method.Async ? "invokeAsync" : "invoke";
        var body = $"{exports}.{invoke}({BuildMethodArgs()})";
        var js = $"{exports}.{method.Namespace}.{method.Name} = ({funcArgs}) => {body};";
        return EnsureNamespaceObjectsDeclared(method.Namespace, js);

        string BuildFuncArgs () => string.Join(", ", method.Arguments.Select(a => a.Name));
        string BuildMethodArgs () => $"'{method.Assembly}', '{method.Name}'" + (funcArgs == "" ? "" : $", {funcArgs}");
    }

    private string GenerateFunctionDeclaration (Method method)
    {
        var js = $"{exports}.{method.Namespace}.{method.Name} = undefined;";
        return EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string EnsureNamespaceObjectsDeclared (string space, string js)
    {
        var objects = BuildObjectNamesForNamespace(space);
        foreach (var obj in objects)
            if (declaredObjects.Add(obj))
                js = JoinLines($"{exports}.{obj} = {{}};", js);
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
