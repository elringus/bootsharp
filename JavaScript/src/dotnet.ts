import { boot, getBootStatus, terminate, BootStatus, BootData } from "./boot";
import { invoke, invokeAsync, createObjectReference, disposeObjectReference, createStreamReference } from "./interop";
import { Assembly } from "./mono";
import { Event } from "./event";

export type BootUris = {
    wasm: string;
    entryAssembly: string;
    assemblies: string[]
}

export const dotnet = {
    Event: Event,
    BootStatus: BootStatus,
    getBootStatus: getBootStatus,
    boot: boot,
    bootUris: undefined as BootUris | undefined,
    terminate: terminate,
    invoke: invoke,
    invokeAsync: invokeAsync,
    createObjectReference: createObjectReference,
    disposeObjectReference: disposeObjectReference,
    createStreamReference: createStreamReference
};

export {
    BootStatus,
    BootData,
    Assembly
};

export {
    boot,
    getBootStatus,
    terminate,
    invoke,
    invokeAsync,
    createObjectReference,
    disposeObjectReference,
    createStreamReference
};
