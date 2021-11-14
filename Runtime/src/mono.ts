import { wasm } from "./wasm";

export interface Assembly {
    name: string;
    data: Uint8Array;
}

export function initializeMono(assemblies: Assembly[]): void {
    assemblies.forEach(loadAssembly);
    wasm.ccall("mono_wasm_load_runtime", null, ["string", "number"], ["", 0]);
    wasm.MONO.mono_wasm_runtime_ready();
    wasm.MONO.mono_wasm_setenv("TZ", "UTC");
    wasm.MONO.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
}

export async function callEntryPoint(assemblyName: string): Promise<any> {
    await wasm.BINDING.call_assembly_entry_point(assemblyName, [[]], "m");
}

function loadAssembly(assembly: Assembly): void {
    const dataLength = assembly.data.length;
    const heapAddress = wasm._malloc(dataLength);
    const heapMemory = new Uint8Array(wasm.HEAPU8.buffer, heapAddress, dataLength);
    heapMemory.set(assembly.data);
    wasm.ccall("mono_wasm_add_assembly", null, ["string", "number", "number"], [assembly.name, heapAddress, dataLength]);
}
