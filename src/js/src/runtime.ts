import type { RuntimeAPI } from "./modules";

let runtime: RuntimeAPI;

/** Assigns the initialized .NET runtime to be used by Bootsharp. */
export function setRuntime(value: RuntimeAPI) {
    runtime = value;
}

export function getHeap(): Uint8Array {
    return runtime.localHeapViewU8();
}

export function malloc(size: number): number {
    return Number(runtime.Module._malloc(size));
}

export function free(ptr: number): void {
    runtime.Module._free(ptr as never);
}
