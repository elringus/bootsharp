import { Assembly } from "./mono";
export interface BootData {
    wasmBinary: Uint8Array;
    assemblies: Assembly[];
    entryAssemblyName: string;
}
export declare enum BootStatus {
    Standby = "Standby",
    Booting = "Booting",
    Terminating = "Terminating",
    Booted = "Booted"
}
export declare function getBootStatus(): BootStatus;
export declare function boot(data: BootData): Promise<void>;
export declare function terminate(): Promise<void>;
