const assert = require("assert");
const dotnet = require("../dist/Microsoft.JSInterop.js").DotNet;

describe("export", () => {
    describe("default library object", () => {
        it("is defined after import", () => assert(dotnet));
    });
    describe("system api", () => {
        it("is defined after import", () => {
            assert(dotnet.attachDispatcher);
            assert(dotnet.jsCallDispatcher);
            assert(dotnet.attachReviver);
        });
    });
    describe("invoke api", () => {
        it("is defined after import", () => {
            assert(dotnet.invokeMethod);
            assert(dotnet.invokeMethodAsync);
        });
    });
    describe("object api", () => {
        it("is defined after import", () => {
            assert(dotnet.createJSObjectReference);
            assert(dotnet.disposeJSObjectReference);
            assert(dotnet.createJSStreamReference);
        });
    });
});
