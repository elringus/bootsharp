const assert = require("assert");
const { packed, getGeneratedTypes, getGeneratedMap } = require("./csharp");

describe("packed library", () => {
    after(packed.terminate);
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
    it("exports C# methods grouped under assembly object", async () => {
        assert.deepStrictEqual(packed.Test.Main.JoinStrings("a", "b"), "ab");
        assert.deepStrictEqual(await packed.Test.Main.JoinStringsAsync("c", "d"), "cd");
    });
    it("when function is not implemented error is thrown", async () => {
        assert.throws(() => packed.Test.Main.TestEchoFunction(""), /.*JSException/);
        assert.throws(() => packed.Test.Types.CountTotalSpeed(), /.*JSException/);
    });
    it("can implement functions declared in C#", async () => {
        packed.Test.Main.EchoFunction = value => value;
        packed.Test.Types.GetRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
        assert.deepStrictEqual(packed.Test.Main.TestEchoFunction("a"), "a");
        assert.deepStrictEqual(packed.Test.Types.CountTotalSpeed(), 3);
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
