import { dotnet as dotnetJS, MonoConfig, RuntimeAPI, exit } from "./dotnet";

let wasm: RuntimeAPI | undefined;

export async function initializeWasm(wasmBinary: Uint8Array, mainAssemblyName: string) {
    const dotnet = global.dotnetJS as typeof dotnetJS;
    if (!dotnet) throw Error("DotNet WASM wrapper is not available.");
    const config = buildConfig(wasmBinary, mainAssemblyName);
    wasm = await dotnet.withConfig(config).create();
    return wasm;
}

export function destroyWasm(): void {
    // https://github.com/Elringus/DotNetJS/issues/22
    exit(0);
    wasm = undefined;
}

function buildConfig(wasmBinary: Uint8Array, mainAssemblyName: string): MonoConfig {
    return {
        mainAssemblyName: mainAssemblyName,
        assets: [
            {
                behavior: "dotnetwasm",
                buffer: wasmBinary,
                name: "dotnet.wasm"
            }
        ],

        remoteSources: [],
        pthreadPoolSize: 0
    };
}
