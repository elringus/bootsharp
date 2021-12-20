using System.Collections.Generic;
using System.Linq;
using static Packer.Utilities;

namespace Packer;

internal class SourceGenerator
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

    private readonly HashSet<string> declaredAssemblies = new();

    public string Generate (string runtimeJS, string runtimeWasm, string entryName, AssemblyInspector inspector)
    {
        declaredAssemblies.Clear();
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
        var args = BuildArgs(method);
        var invoke = method.Async ? "invokeAsync" : "invoke";
        var body = $"{exports}.{invoke}('{method.Assembly}', '{method.Name}', {args})";
        var js = $"{exports}.{method.Assembly}.{method.Name} = ({args}) => {body};";
        return EnsureAssemblyDeclared(method.Assembly, js);
    }

    private string GenerateFunctionDeclaration (Method method)
    {
        var js = $"{exports}.{method.Assembly}.{method.Name} = undefined;";
        return EnsureAssemblyDeclared(method.Assembly, js);
    }

    private string BuildArgs (Method method)
    {
        var names = method.Arguments.Select(a => a.Name);
        return string.Join(", ", names);
    }

    private string GenerateFunctionBinding (Method method)
    {
        var global = $"global.DotNetJS_functions_{method.Assembly.Replace('.', '_')}_{method.Name}";
        var error = $"throw new Error(\"Function 'dotnet.{method.Assembly}.{method.Name}' is not implemented.\");";
        return $"{global} = {exports}.{method.Assembly}.{method.Name} || function() {{ {error} }}();";
    }

    private string EnsureAssemblyDeclared (string assembly, string js)
    {
        if (declaredAssemblies.Add(assembly))
            js = JoinLines(GenerateDeclarationsForAssembly(assembly), js);
        return js;
    }

    private string GenerateDeclarationsForAssembly (string assembly)
    {
        var parts = assembly.Split('.');
        var declarations = "";
        for (int i = 0; i < parts.Length; i++)
        {
            var previousParts = i > 0 ? string.Join(".", parts.Take(i)) + "." : "";
            var path = previousParts + parts[i];
            declarations = JoinLines(declarations, $"{exports}.{path} = {{}};");
        }
        return declarations;
    }
}
