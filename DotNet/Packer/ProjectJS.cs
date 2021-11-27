using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetJS.Packer
{
    public class ProjectJS
    {
        private const string library = "dotnet";
        private const string invokableAttribute = "JSInvokableAttribute";
        private const string functionAttribute = "JSFunctionAttribute";
        private const string assemblyTemplate = "{ name: '%NAME%', data: '%DATA%' }";
        private const string moduleTemplate = @"
(function (root, factory) {
    if (typeof exports === 'object' && typeof exports.nodeName !== 'string')
        factory(module.exports);
    else factory(root.dotnet);
}(typeof self !== 'undefined' ? self : this, function (exports) {
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

        private readonly string entryName;
        private readonly string wasmBase64;
        private readonly IReadOnlyList<Assembly> assemblies;

        public ProjectJS (string entryName, string wasmBase64, IReadOnlyList<Assembly> assemblies)
        {
            this.entryName = entryName;
            this.wasmBase64 = wasmBase64;
            this.assemblies = assemblies;
        }

        public string Generate ()
        {
            var initJS = GenerateInitJS(assemblies);
            var bootJS = GenerateBootJS(assemblies);
            var dlls = string.Join(",", assemblies.Select(GenerateAssembly));
            return moduleTemplate
                .Replace("%ENTRY%", entryName)
                .Replace("%WASM%", wasmBase64)
                .Replace("%DLLS%", dlls)
                .Replace("%INIT_JS%", initJS)
                .Replace("%BOOT_JS%", bootJS);
        }

        private string GenerateAssembly (Assembly assembly)
        {
            return assemblyTemplate
                .Replace("%NAME%", assembly.Name)
                .Replace("%DATA%", assembly.Base64);
        }

        private string GenerateInitJS (IReadOnlyList<Assembly> assemblies)
        {
            var invokableMethods = assemblies.SelectMany(a => GetMethodsWithAttribute(a, invokableAttribute));
            var functionMethods = assemblies.SelectMany(a => GetMethodsWithAttribute(a, functionAttribute));
            return string.Join("\n", invokableMethods.Select(GenerateInvokableBinding)) + "\n" +
                   string.Join("\n", functionMethods.Select(GenerateFunctionDeclaration));
        }

        private string GenerateBootJS (IReadOnlyList<Assembly> assemblies)
        {
            var functionMethods = assemblies.SelectMany(a => GetMethodsWithAttribute(a, functionAttribute));
            return string.Join("\n", functionMethods.Select(GenerateFunctionBinding));
        }

        private MethodInfo[] GetMethodsWithAttribute (Assembly assembly, string attribute)
        {
            return System.Reflection.Assembly.ReflectionOnlyLoad(assembly.Bytes).GetExportedTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == attribute))
                .ToArray();
        }

        private string GenerateInvokableBinding (MethodInfo invokableMethod)
        {
            var name = invokableMethod.Name;
            if (invokableMethod.DeclaringType is null)
                throw new PackerException($"Failed to generate JavaScript binding for '{name}' method.");
            var assembly = invokableMethod.DeclaringType.FullName;
            var args = GetArgs(invokableMethod);
            var invoke = GetInvokeFunction(invokableMethod);
            return $"{library}.{assembly} = {library}.{assembly} || {{}}; " +
                   $"{library}.{assembly}.{name} = ({args}) => {library}.{invoke}('{assembly}', '{name}', {args});";
        }

        private string GenerateFunctionDeclaration (MethodInfo functionMethod)
        {
            return "";
        }

        private string GenerateFunctionBinding (MethodInfo functionMethod)
        {
            return "";
        }

        private string GetArgs (MethodInfo method)
        {
            return string.Join(", ", method.GetParameters().Select(p => p.Name));
        }

        private string GetInvokeFunction (MethodInfo method)
        {
            var awaitable = method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            return awaitable ? "invokeAsync" : "invoke";
        }
    }
}
