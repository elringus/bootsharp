import { initializeWasm, destroyWasm } from "./wasm";
import { initializeInterop } from "./interop";
import { Assembly, initializeMono, callEntryPoint } from "./mono";
import { Base64 } from "js-base64";

export interface BootData {
    wasm: Uint8Array | string;
    assemblies: Assembly[];
    entryAssemblyName: string;
}

export enum BootStatus {
    Standby = "Standby",
    Booting = "Booting",
    Terminating = "Terminating",
    Booted = "Booted"
}

let bootStatus: BootStatus = BootStatus.Standby;

export function getBootStatus(): BootStatus {
    return bootStatus;
}

export async function boot(bootData: BootData): Promise<void> {
    validateBootData(bootData);
    transitionBootStatus(BootStatus.Standby, BootStatus.Booting);
    await initializeWasm(getWasmBinary(bootData.wasm));
    initializeMono(bootData.assemblies);
    initializeInterop();
    await callEntryPoint(bootData.entryAssemblyName);
    transitionBootStatus(BootStatus.Booting, BootStatus.Booted);
}

export function terminate(): Promise<void> {
    transitionBootStatus(BootStatus.Booted, BootStatus.Terminating);
    destroyWasm();
    transitionBootStatus(BootStatus.Terminating, BootStatus.Standby);
    return Promise.resolve();
}

function transitionBootStatus(from: BootStatus, to: BootStatus): void {
    if (from !== bootStatus)
        throw Error(`Invalid boot status. Expected: ${from}. Actual: ${bootStatus}.`);
    bootStatus = to;
}

function getWasmBinary(wasm: Uint8Array | string) {
    return typeof wasm === "string" ? Base64.toUint8Array(wasm) : wasm;
}

function validateBootData(data: BootData): void {
    if (data == null)
        throw new Error("Boot data is missing.");
    if (data.wasm == null || data.wasm.length == 0)
        throw new Error("Wasm binary is missing.");
    for (const assembly of data.assemblies)
        if (assembly.data == null || assembly.data.length == 0)
            throw new Error(`${assembly.name} assembly data is invalid.`);
}
