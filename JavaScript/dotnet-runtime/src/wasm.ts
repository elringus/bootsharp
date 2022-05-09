import factory, { DotNetModule } from "./native/dotnet";

export let wasm: DotNetModule;

export async function initializeWasm(wasmBinary: Uint8Array): Promise<void> {
    wasm = await factory({ wasmBinary });
}

export function destroyWasm(): void {
    // https://github.com/Elringus/DotNetJS/issues/22
    // EM.ccall("emscripten_force_exit", null, ["number"], [0]);
}
