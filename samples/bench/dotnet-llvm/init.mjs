import { dotnet } from "./bin/Release/net9.0-browser/browser-wasm/publish/dotnet.js";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    const runtime = await dotnet.withDiagnosticTracing(false).create();
    await runtime.runMain("DotNetLLVM", []);

    return {
        echoNumber: runtime.Module._echoNumber,
        echoStruct: () => JSON.parse(runtime.Module.UTF8ToString(runtime.Module._echoStruct())),
        fi: runtime.Module._fi
    };
}
