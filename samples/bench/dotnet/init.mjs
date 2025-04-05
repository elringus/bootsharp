import { dotnet } from "./bin/Release/net9.0/browser-wasm/AppBundle/_framework/dotnet.js";
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    const runtime = await dotnet.withDiagnosticTracing(false).create();
    const asm = runtime.getConfig().mainAssemblyName;

    runtime.setModuleImports("x", {
        getNumber,
        getStruct: () => JSON.stringify(getStruct())
    });

    await runtime.runMain(asm, []);

    const exports = await runtime.getAssemblyExports(asm);
    return {
        echoNumber: exports.Program.EchoNumber,
        echoStruct: () => JSON.parse(exports.Program.EchoStruct()),
        fi: exports.Program.Fi
    };
}
