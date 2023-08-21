// Based on https://github.com/dotnet/aspnetcore/blob/release/6.0/src/Components/Web.JS/src/Platform/Mono/MonoPlatform.ts

import { DotNet } from "./dotnet-js-interop";
import { getBootStatus, BootStatus } from "./boot";
import { RuntimeAPI } from "./dotnet";

export async function initializeInterop(wasm: RuntimeAPI) {
    const exports = await wasm.getAssemblyExports("Microsoft.AspNetCore.Components.WebAssembly");
    console.log(exports);
    // DotNet.attachDispatcher(createDispatcher(exports));
}

export const invoke: <T>(assembly: string, method: string, ...args: any[]) => T = whenBooted(DotNet.invokeMethod);
export const invokeAsync: <T>(assembly: string, method: string, ...args: any[]) => Promise<T> = whenBooted(DotNet.invokeMethodAsync);
export const createObjectReference: (object: any) => any = whenBooted(DotNet.createJSObjectReference);
export const disposeObjectReference: (objectReference: any) => void = whenBooted(DotNet.disposeJSObjectReference);
export const createStreamReference: (buffer: Uint8Array | any) => any = whenBooted(DotNet.createJSStreamReference);

function whenBooted<T extends Function>(fn: T): T {
    return <any>function (...args) {
        const status = getBootStatus();
        if (status !== BootStatus.Booted)
            throw Error(`Can't interop until .NET runtime is booted. Current status: ${status}.`);
        return fn(...args);
    };
}

// function bindMethods(exports: any): void {
//     invokeDotNet = bindStaticMethod("Microsoft.AspNetCore.Components.WebAssembly",
//         "Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime", "InvokeDotNet");
//     beginInvokeDotNet = bindStaticMethod("Microsoft.AspNetCore.Components.WebAssembly",
//         "Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime", "BeginInvokeDotNet");
//     endInvokeJS = bindStaticMethod("Microsoft.AspNetCore.Components.WebAssembly",
//         "Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime", "EndInvokeJS");
//     notifyByteArrayAvailable = bindStaticMethod("Microsoft.AspNetCore.Components.WebAssembly",
//         "Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime", "NotifyByteArrayAvailable");
// }
//
// function createDispatcher(exports: any): DotNet.DotNetCallDispatcher {
//     return {
//         invokeDotNetFromJS: invokeDotNetFromJS,
//         beginInvokeDotNetFromJS: beginInvokeDotNetFromJS,
//         endInvokeJSFromDotNet: endInvokeJSFromDotNet,
//         sendByteArray: sendByteArray
//     };
// }
