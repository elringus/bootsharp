const assert = require("assert");
const dotnet = require("../dist/dotnet");

describe("export", () => {
    describe("runtime default", () => {
        it("is defined after import", () => {
            assert(dotnet.BootStatus);
            assert(dotnet.getBootStatus);
            assert(dotnet.boot);
            assert(dotnet.terminate);
            assert(dotnet.invoke);
            assert(dotnet.invokeAsync);
            assert(dotnet.createObjectReference);
            assert(dotnet.disposeObjectReference);
            assert(dotnet.createStreamReference);
        });
    });
    // Below globals are required for emscripten and blazor internals.
    // TODO: Find a way to hide them from leaking to consumer global space.
    describe("blazor internal", () => {
        it("is defined after import", () => {
            const blazor = global["Blazor"]._internal;
            assert(blazor.invokeJSFromDotNet);
            assert(blazor.endInvokeDotNetFromJS);
            assert(blazor.receiveByteArray);
            assert(blazor.retrieveByteArray);
        });
    });
    describe("emscripten module", () => {
        it("is defined after import", () => {
            const em = global["Module"];
            assert(em.wasmBinary);
        });
    });
    describe("dotnet interop", () => {
        it("is defined after import", () => {
            const dotnet = global["DotNet"];
            assert(dotnet.attachDispatcher);
            assert(dotnet.invokeMethodAsync);
        });
    });
});
