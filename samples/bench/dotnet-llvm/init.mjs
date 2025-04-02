import { dotnet } from "./bin/Release/net9.0-browser/browser-wasm/publish/dotnet.js";
import { getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    const runtime = await dotnet.withDiagnosticTracing(false).create();
    const asm = "DotNetLLVM";

    runtime.setModuleImports("x", {
        getStruct: () => JSON.stringify(getStruct())
    });

    await runtime.runMain(asm, []);

    const exports = await runtime.getAssemblyExports(asm);
    return {
        echoNumber: runtime.Module._echoNumber,
        echoStruct: exports.Program.EchoStruct,
        fi: exports.Program.Fi
    };
}
