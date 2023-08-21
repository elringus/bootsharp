import { boot, getBootStatus, terminate, BootStatus, BootData } from "./boot";
import { invoke, invokeAsync, createObjectReference, disposeObjectReference, createStreamReference } from "./interop";
import { injectCrypto } from "./crypto";
import { Event } from "./event";

export const bootsharp = {
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
    BootData
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

// https://github.com/Elringus/DotNetJS/issues/17
injectCrypto();
