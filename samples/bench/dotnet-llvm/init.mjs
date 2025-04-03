import { dotnet } from "./bin/Release/net9.0-browser/browser-wasm/publish/dotnet.js";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    const runtime = await dotnet.withDiagnosticTracing(false).create();
    const asm = "DotNetLLVM";
    await runtime.runMain(asm, []);
    const exports = await runtime.getAssemblyExports(asm);
    return {
        echoNumber: runtime.Module._echoNumber,
        echoStruct: exports.Program.EchoStruct,
        fi: runtime.Module._fi
    };
}
