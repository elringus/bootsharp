// noinspection JSCheckFunctionSignatures,JSUnresolvedFunction,JSUnresolvedVariable

const assert = require("assert");
const { packed, getGeneratedTypes, getGeneratedMap } = require("./csharp");

describe("packed library", () => {
    after(packed.terminate);
    it("throws on boot when a C#-declared function is missing implementation", async () => {
        await assert.rejects(packed.boot, /Function 'dotnet.Test.Main.EchoFunction' is not implemented\./);
    });
    it("allows providing implementation for functions declared in C#", () => {
        packed.Test.Main.EchoFunction = value => value;
        packed.Test.Types.GetRegistry = () => {};
    });
    it("can boot without specifying boot data", async () => {
        await assert.doesNotReject(packed.boot);
        assert.deepStrictEqual(packed.getBootStatus(), packed.BootStatus.Booted);
    });
    it("re-exports dotnet members", async () => {
        assert(packed.BootStatus instanceof Object);
        assert(packed.getBootStatus instanceof Function);
        assert(packed.terminate instanceof Function);
        assert(packed.invoke instanceof Function);
        assert(packed.invokeAsync instanceof Function);
        assert(packed.createObjectReference instanceof Function);
        assert(packed.disposeObjectReference instanceof Function);
        assert(packed.createStreamReference instanceof Function);
    });
    it("provides exposed C# methods grouped under assembly object", async () => {
        assert.deepStrictEqual(packed.Test.Main.JoinStrings("a", "b"), "ab");
        assert.deepStrictEqual(await packed.Test.Main.JoinStringsAsync("c", "d"), "cd");
    });
    it("can interop via functions declared in C#", async () => {
        assert.deepStrictEqual(packed.Test.Main.TestEchoFunction("a"), "a");
    });
    it("still can interop via strings", async () => {
        assert.deepStrictEqual(packed.invoke("Test.Main", "JoinStrings", "a", "b"), "ab");
        assert.deepStrictEqual(await packed.invokeAsync("Test.Main", "JoinStringsAsync", "a", "b"), "ab");
    });
    it("generates type definitions", () => {
        assert(getGeneratedTypes());
    });
    it("generates source map", () => {
        assert(getGeneratedMap());
    });
});
