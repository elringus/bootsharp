// const assert = require("node:assert");
// const { describe, it } = require("bun:test");
// const dotnet = require("../dist/dotnet");
//
// describe("export", () => {
//     describe("runtime default", () => {
//         it("is defined after import", () => {
//             assert(dotnet.BootStatus);
//             assert(dotnet.getBootStatus);
//             assert(dotnet.boot);
//             assert(dotnet.terminate);
//             assert(dotnet.invoke);
//             assert(dotnet.invokeAsync);
//             assert(dotnet.createObjectReference);
//             assert(dotnet.disposeObjectReference);
//             assert(dotnet.createStreamReference);
//         });
//     });
//     describe("crypto", () => {
//         it("is defined after import", () => {
//             const crypto = globalThis.crypto;
//             assert(crypto.getRandomValues);
//         });
//     });
//     // Below globals are required for emscripten and blazor internals.
//     describe("blazor internal", () => {
//         it("is defined after import", () => {
//             const blazor = globalThis.Blazor._internal;
//             assert(blazor.invokeJSFromDotNet);
//             assert(blazor.endInvokeDotNetFromJS);
//             assert(blazor.receiveByteArray);
//             assert(blazor.retrieveByteArray);
//             assert(blazor.getJSDataStreamChunk);
//         });
//     });
//     describe("emscripten module", () => {
//         it("is defined after import", () => {
//             const em = globalThis.Module;
//             assert(em.wasmBinary);
//         });
//     });
//     describe("dotnet interop", () => {
//         it("is defined after import", () => {
//             const dotnet = globalThis.DotNet;
//             assert(dotnet.attachDispatcher);
//             assert(dotnet.invokeMethodAsync);
//         });
//     });
// });
