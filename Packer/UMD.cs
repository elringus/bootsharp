using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DotNetJS.Packer
{
    public static class UMD
    {
        private const string moduleTemplate = @"
(function (root, factory) {
    if (typeof define === 'function' && define.amd)
        define(['exports', 'dotnet'], factory);
    else if (typeof exports === 'object' && typeof exports.nodeName !== 'string')
        factory(module.exports, { boot: module.exports.boot, invoke: module.exports.invoke, invokeAsync: module.exports.invokeAsync });
    else factory((root.%LIBRARY% = {}), root.dotnet);
}(typeof self !== 'undefined' ? self : this, function (exports, dotnet) {
    exports.boot = async function () {
        const bootData = {
            wasm: '%WASM%',
            assemblies: [%DLLS%],
            entryAssemblyName: '%ENTRY%'
        };
        await dotnet.boot(bootData);
    };
    exports.invoke = (name, ...args) => dotnet.invoke('%LIBRARY%', name, ...args);
    exports.invokeAsync = (name, ...args) => dotnet.invokeAsync('%LIBRARY%', name, ...args);
}));";

        private const string assemblyTemplate = "{ name: '%NAME%', data: '%DATA%' }";

        public static string GenerateJS (string entryName, string wasmBase64, IEnumerable<Assembly> assemblies)
        {
            var libraryName = Path.GetFileNameWithoutExtension(entryName);
            var dlls = string.Join(",", assemblies.Select(GenerateAssembly));
            return moduleTemplate
                .Replace("%ENTRY%", entryName)
                .Replace("%LIBRARY%", libraryName)
                .Replace("%WASM%", wasmBase64)
                .Replace("%DLLS%", dlls);
        }

        private static string GenerateAssembly (Assembly assembly)
        {
            return assemblyTemplate
                .Replace("%NAME%", assembly.Name)
                .Replace("%DATA%", assembly.Base64);
        }
    }
}
