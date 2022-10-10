import { wasm } from "./wasm";
import { Base64 } from "js-base64";

export interface Assembly {
    name: string;
    data: Uint8Array | string;
}

export function initializeMono(assemblies: Assembly[]): void {
    assemblies.forEach(loadAssembly);
    wasm.ccall("mono_wasm_load_runtime", null, ["string", "number"], ["", 0]);
    wasm.MONO.mono_wasm_runtime_ready();
    wasm.MONO.mono_wasm_setenv("TZ", "UTC");
    wasm.MONO.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
    injectCrypto();
}

export async function callEntryPoint(assemblyName: string): Promise<any> {
    await wasm.BINDING.call_assembly_entry_point(assemblyName, [[]], "m");
}

function loadAssembly(assembly: Assembly): void {
    const dataLength = assembly.data.length;
    const heapAddress = wasm._malloc(dataLength);
    const heapMemory = new Uint8Array(wasm.HEAPU8.buffer, heapAddress, dataLength);
    const data = typeof assembly.data === "string" ? Base64.toUint8Array(assembly.data) : assembly.data;
    heapMemory.set(data);
    wasm.ccall("mono_wasm_add_assembly", null, ["string", "number", "number"], [assembly.name, heapAddress, dataLength]);
}

function injectCrypto(): void {
    if (typeof crypto !== "undefined" && "getRandomValues" in crypto) return;
    // https://github.com/Elringus/DotNetJS/issues/17
    globalThis.crypto = {
        getRandomValues: getRandomValues,
        randomUUID: randomUUID
    } as any;

    function getRandomValues(buffer: Uint8Array): Uint8Array {
        for (let i = 0; i < buffer.length; i++)
            buffer[i] = (Math.random() * 256) | 0;
        return buffer;
    }

    function randomUUID() {
        return (
            [1e7] as any + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g,
            c => (c ^ getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
        );
    }
}
