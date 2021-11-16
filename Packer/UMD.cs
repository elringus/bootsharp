namespace DotNetJS.Packer
{
    public static class UMD
    {
        private const string template = @"



(function (root, factory) {
    if (typeof define === 'function' && define.amd)
        define(['exports', 'dotnet'], factory);
    else if (typeof exports === 'object' && typeof exports.nodeName !== 'string')
        factory(exports, require('dotnet'));
    else factory((root.%LIBRARY% = {}), root.b);
}(typeof self !== 'undefined' ? self : this, async function (exports, dotnet) {
    exports.boot = async function () {
        const bootData = {
            wasm: loadWasmBinary(),
            assemblies: loadAssemblies(),
            entryAssemblyName: 'Test.dll'
        };
        await dotnet.boot(bootData);
    };
    exports.invoke = (method, ...args) => dotnet.invoke(%LIBRARY%, method, ...args);
    exports.invokeAsync = (name, ...args) => dotnet.invokeAsync(%LIBRARY%, name, ...args);
}}));";

        public static string GenerateJS (string libraryName, string wasmBase64, string[] dllsBase64)
        {
            return null;
        }
    }
}
