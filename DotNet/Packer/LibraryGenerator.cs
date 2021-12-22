using System.Collections.Generic;
using System.Linq;
using static Packer.Utilities;

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
        %BOOT_JS%
        const bootData = {
            wasm: '%WASM%',
            assemblies: [%DLLS%],
            entryAssemblyName: '%ENTRY%'
        };
        await bootWithData(bootData);
    };
}));";

    private readonly HashSet<string> declaredObjects = new();

    public string Generate (string runtimeJS, string runtimeWasm, string entryName, AssemblyInspector inspector)
    {
        declaredObjects.Clear();
        var initJS = GenerateInitJS(inspector.InvokableMethods, inspector.FunctionMethods);
        var bootJS = GenerateBootJS(inspector.FunctionMethods);
        var dlls = string.Join(", ", inspector.Assemblies.Select(GenerateAssembly));
        return moduleTemplate
            .Replace("%RUNTIME_JS%", runtimeJS)
            .Replace("%ENTRY%", entryName)
            .Replace("%WASM%", runtimeWasm)
            .Replace("%DLLS%", dlls)
            .Replace("%INIT_JS%", initJS)
            .Replace("%BOOT_JS%", bootJS);
    }

    private string GenerateAssembly (Assembly assembly)
    {
        return $"{{ name: '{assembly.Name}', data: '{assembly.Base64}' }}";
    }

    private string GenerateInitJS (IEnumerable<Method> invokable, IEnumerable<Method> functions)
    {
        return JoinLines(
            JoinLines(invokable.Select(GenerateInvokableBinding)),
            JoinLines(functions.Select(GenerateFunctionDeclaration))
        );
    }

    private string GenerateBootJS (IEnumerable<Method> functions)
    {
        return JoinLines(functions.Select(GenerateFunctionBinding), 2);
    }

    private string GenerateInvokableBinding (Method method)
    {
        var funcArgs = BuildFuncArgs();
        var invoke = method.Async ? "invokeAsync" : "invoke";
        var body = $"{exports}.{invoke}({BuildMethodArgs()})";
        var js = $"{exports}.{method.Assembly}.{method.Name} = ({funcArgs}) => {body};";
        return EnsureAssemblyObjectsDeclared(method.Assembly, js);

        string BuildFuncArgs () => string.Join(", ", method.Arguments.Select(a => a.Name));
        string BuildMethodArgs () => $"'{method.Assembly}', '{method.Name}'" + (funcArgs == "" ? "" : $", {funcArgs}");
    }

    private string GenerateFunctionDeclaration (Method method)
    {
        var js = $"{exports}.{method.Assembly}.{method.Name} = undefined;";
        return EnsureAssemblyObjectsDeclared(method.Assembly, js);
    }

    private string GenerateFunctionBinding (Method method)
    {
        var global = $"global.DotNetJS_functions_{method.Assembly.Replace('.', '_')}_{method.Name}";
        var error = $"throw new Error(\"Function 'dotnet.{method.Assembly}.{method.Name}' is not implemented.\");";
        return $"{global} = {exports}.{method.Assembly}.{method.Name} || function() {{ {error} }}();";
    }

    private string EnsureAssemblyObjectsDeclared (string assembly, string js)
    {
        var objects = BuildObjectNamesForAssembly(assembly);
        foreach (var obj in objects)
            if (declaredObjects.Add(obj))
                js = JoinLines($"{exports}.{obj} = {{}};", js);
        return js;
    }

    private List<string> BuildObjectNamesForAssembly (string assembly)
    {
        var parts = assembly.Split('.');
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
