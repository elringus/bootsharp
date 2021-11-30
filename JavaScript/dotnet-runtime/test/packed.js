// noinspection JSUnresolvedFunction,JSCheckFunctionSignatures,JSUnresolvedVariable

const assert = require("assert");
const dotnet = require("./project/bin/dotnet");
const { getGeneratedTypes } = require("./project");

describe("packed library", () => {
    after(dotnet.terminate);
    it("throws on boot when a C#-declared function is missing implementation", async () => {
        await assert.rejects(dotnet.boot, /Function 'dotnet.Test.Project.EchoFunction' is not implemented\./);
    });
    it("allows providing implementation for functions declared in C#", () => {
        dotnet.Test.Project.EchoFunction = value => value;
    });
    it("can boot without specifying boot data", async () => {
        await assert.doesNotReject(dotnet.boot);
        assert.deepStrictEqual(dotnet.getBootStatus(), dotnet.BootStatus.Booted);
    });
    it("re-exports dotnet members", async () => {
        assert(dotnet.BootStatus instanceof Object);
        assert(dotnet.getBootStatus instanceof Function);
        assert(dotnet.terminate instanceof Function);
        assert(dotnet.invoke instanceof Function);
        assert(dotnet.invokeAsync instanceof Function);
        assert(dotnet.createObjectReference instanceof Function);
        assert(dotnet.disposeObjectReference instanceof Function);
        assert(dotnet.createStreamReference instanceof Function);
    });
    it("provides exposed C# methods grouped under assembly object", async () => {
        assert.deepStrictEqual(dotnet.Test.Project.JoinStrings("a", "b"), "ab");
        assert.deepStrictEqual(await dotnet.Test.Project.JoinStringsAsync("c", "d"), "cd");
    });
    it("can interop via functions declared in C#", async () => {
        assert.deepStrictEqual(dotnet.Test.Project.TestEchoFunction("a"), "a");
    });
    it("still can interop via strings", async () => {
        assert.deepStrictEqual(dotnet.invoke("Test.Project", "JoinStrings", "a", "b"), "ab");
        assert.deepStrictEqual(await dotnet.invokeAsync("Test.Project", "JoinStringsAsync", "a", "b"), "ab");
    });
    it("generates valid type definitions", async () => {
        // TODO: Find a way to check validity of the generated types.
        assert(getGeneratedTypes());
    });
});
