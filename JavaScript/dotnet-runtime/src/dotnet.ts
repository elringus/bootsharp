import { BootStatus, getBootStatus, boot, terminate, type BootData } from "./boot";
import { invoke, invokeAsync, createObjectReference, disposeObjectReference, createStreamReference } from "./interop";
import type { Assembly } from "./mono";

export const dotnet = {
    Event: Event,
    BootStatus: BootStatus,
    getBootStatus: getBootStatus,
    boot: boot,
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
