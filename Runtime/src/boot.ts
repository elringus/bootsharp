import { initializeWasm, destroyWasm } from "./wasm";
import { initializeInterop } from "./interop";
import { Assembly, initializeMono, callEntryPoint } from "./mono";

export interface BootData {
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

export async function boot(data: BootData): Promise<void> {
    validateBootData(data);
    transitionBootStatus(BootStatus.Standby, BootStatus.Booting);
    await initializeWasm();
    initializeMono(data.assemblies);
    initializeInterop();
    await callEntryPoint(data.entryAssemblyName);
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

function validateBootData(data: BootData): void {
    if (data == null)
        throw new Error("Boot data is missing.");
    if (data.assemblies.length == 0)
        throw new Error("Boot assemblies are missing.");
    if (!data.assemblies.some(a => a.name == data.entryAssemblyName))
        throw new Error("Entry assembly is not found.");
    for (const assembly of data.assemblies)
        if (assembly.data == null || assembly.data.length == 0)
            throw new Error(`${assembly.name} assembly data is invalid.`);
}
