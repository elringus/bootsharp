using System.Collections.Generic;
using System.Linq;

namespace DotNetJS.Packer
{
    public static class UMD
    {
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

        private const string assemblyTemplate = "{ name: '%NAME%', data: '%DATA%' }";

        public static string GenerateJS (string entryName, string wasmBase64, 
            IEnumerable<Assembly> assemblies, string initJS, string bootJS)
        {
            var dlls = string.Join(",", assemblies.Select(GenerateAssembly));
            return moduleTemplate
                .Replace("%ENTRY%", entryName)
                .Replace("%WASM%", wasmBase64)
                .Replace("%DLLS%", dlls)
                .Replace("%INIT_JS%", initJS)
                .Replace("%BOOT_JS%", bootJS);
        }

        private static string GenerateAssembly (Assembly assembly)
        {
            return assemblyTemplate
                .Replace("%NAME%", assembly.Name)
                .Replace("%DATA%", assembly.Base64);
        }
    }
}
