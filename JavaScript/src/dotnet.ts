import { boot, getBootUris, getBootStatus, terminate, BootStatus, BootData, BootUris } from "./boot";
import { invoke, invokeAsync, createObjectReference, disposeObjectReference, createStreamReference } from "./interop";
import { Assembly } from "./mono";
import { Event } from "./event";

export const dotnet = {
    Event: Event,
    BootStatus: BootStatus,
    getBootUris: getBootUris,
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
    BootUris,
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
