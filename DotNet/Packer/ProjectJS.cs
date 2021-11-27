using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DotNetJS.Packer
{
    public class ProjectJS
    {
        private const string exports = "exports";
        private const string invokableAttribute = "JSInvokableAttribute";
        private const string functionAttribute = "JSFunctionAttribute";
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
        private readonly List<MethodInfo> invokableMethods = new List<MethodInfo>();
        private readonly List<MethodInfo> functionMethods = new List<MethodInfo>();
        private readonly HashSet<string> declaredAssemblies = new HashSet<string>();

        public ProjectJS (string entryName, string wasmBase64, IReadOnlyList<Assembly> assemblies)
        {
            this.entryName = entryName;
            this.wasmBase64 = wasmBase64;
            this.assemblies = assemblies;
            foreach (var assembly in assemblies)
                CollectMethods(assembly.Path);
        }

        public string Generate ()
        {
            var initJS = GenerateInitJS();
            var bootJS = GenerateBootJS();
            var dlls = string.Join(", ", assemblies.Select(GenerateAssembly));
            return moduleTemplate
                .Replace("%ENTRY%", entryName)
                .Replace("%WASM%", wasmBase64)
                .Replace("%DLLS%", dlls)
                .Replace("%INIT_JS%", initJS)
                .Replace("%BOOT_JS%", bootJS);
        }

        private void CollectMethods (string assemblyPath)
        {
            try
            {
                var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
                foreach (var method in assembly.GetExportedTypes().SelectMany(t => t.GetMethods()))
                    if (method.CustomAttributes.Any(a => a.AttributeType.Name == invokableAttribute))
                        invokableMethods.Add(method);
                    else if (method.CustomAttributes.Any(a => a.AttributeType.Name == functionAttribute))
                        functionMethods.Add(method);
            }
            catch { return; }
        }

        private string GenerateAssembly (Assembly assembly)
        {
            return $"{{ name: '{assembly.Name}', data: '{assembly.Base64}' }}";
        }

        private string GenerateInitJS ()
        {
            return JoinNewLine(
                JoinNewLine(invokableMethods.Select(GenerateInvokableBinding)),
                JoinNewLine(functionMethods.Select(GenerateFunctionDeclaration))
            );
        }

        private string GenerateBootJS ()
        {
            return JoinNewLine(functionMethods.Select(GenerateFunctionBinding), 2);
        }

        private string GenerateInvokableBinding (MethodInfo invokableMethod)
        {
            var name = invokableMethod.Name;
            var args = GetArgs(invokableMethod);
            var invoke = GetInvokeFunction(invokableMethod);
            var assembly = GetAssemblyName(invokableMethod);
            var body = $"{exports}.{invoke}('{assembly}', '{name}', {args})";
            var js = $"{exports}.{assembly}.{name} = ({args}) => {body};";
            return EnsureAssemblyDeclared(assembly, js);
        }

        private string GenerateFunctionDeclaration (MethodInfo functionMethod)
        {
            var name = functionMethod.Name;
            var args = GetArgs(functionMethod);
            var assembly = GetAssemblyName(functionMethod);
            var js = $"{exports}.{assembly}.{name} = undefined;";
            return EnsureAssemblyDeclared(assembly, js);
        }

        private string GenerateFunctionBinding (MethodInfo functionMethod)
        {
            var name = functionMethod.Name;
            var assembly = GetAssemblyName(functionMethod);
            var error = $"function() {{ throw new Error(\"Function 'dotnet.{assembly}.{name}' is not implemented.\"); }}()";
            return $"global.DotNetJS_functions_{assembly}_{name} = {exports}.{assembly}.{name} || {error};";
        }

        private string EnsureAssemblyDeclared (string assembly, string js)
        {
            if (declaredAssemblies.Add(assembly))
                js = JoinNewLine($"{exports}.{assembly} = {{}};", js);
            return js;
        }

        private string GetAssemblyName (MemberInfo member)
        {
            if (member.DeclaringType is null)
                throw new PackerException($"Failed to get declaring type for '{member}'.");
            return member.DeclaringType.Assembly.GetName().Name;
        }

        private string GetArgs (MethodInfo method)
        {
            return string.Join(", ", method.GetParameters().Select(ToJavaScriptArg));
        }

        private string ToJavaScriptArg (ParameterInfo param)
        {
            switch (param.Name)
            {
                case "function": return "fn";
                default: return param.Name;
            }
        }

        private string GetInvokeFunction (MethodInfo method)
        {
            var awaitable = method.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            return awaitable ? "invokeAsync" : "invoke";
        }

        private string JoinNewLine (IEnumerable<string> values, int indent = 1)
        {
            var separator = "\n" + new string(' ', indent * 4);
            return string.Join(separator, values);
        }

        private string JoinNewLine (params string[] values) => JoinNewLine(values, 1);
    }
}
