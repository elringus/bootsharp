const assert = require("node:assert");
const { describe, it, after } = require("node:test");
const { packed, getGeneratedTypes, getGeneratedMap } = require("./cs");

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
        assert.deepStrictEqual(packed.Test.Main.joinStrings("a", "b"), "ab");
        assert.deepStrictEqual(await packed.Test.Main.joinStringsAsync("c", "d"), "cd");
    });
    it("when function is not implemented error is thrown", async () => {
        assert.throws(() => packed.Test.Main.testEchoFunction(""), /.*JSException/);
        await assert.rejects(() => packed.Test.Main.testAsyncEchoFunction(""), /.*JSException/);
        assert.throws(() => packed.Test.Types.countTotalSpeed(), /.*JSException/);
    });
    it("can implement functions declared in C#", async () => {
        packed.Test.Main.echoFunction = value => value;
        packed.Test.Main.asyncEchoFunction = async value => value;
        packed.Test.Types.getRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
        assert.deepStrictEqual(packed.Test.Main.testEchoFunction("a"), "a");
        assert.deepStrictEqual(await packed.Test.Main.testAsyncEchoFunction("b"), "b");
        assert.deepStrictEqual(packed.Test.Types.countTotalSpeed(), 3);
    });
    it("array args handled correctly", async () => {
        packed.Test.Main.arrayArgFunction = values => values;
        assert.deepStrictEqual(packed.Test.Main.testArrayArgFunction(["a", "b"]), ["a", "b"]);
    });
    it("can subscribe to events declared in C#", async () => {
        let result = "";
        packed.Test.Main.onEventBroadcast.subscribe(v => result = v);
        packed.Test.Main.broadcastEvent("foo");
        assert.deepStrictEqual(result, "foo");
    });
    it("can un-subscribe from events declared in C#", async () => {
        let result = "";
        const assigner = v => result = v;
        packed.Test.Main.onEventBroadcast.subscribe(assigner);
        packed.Test.Main.broadcastEvent("foo");
        packed.Test.Main.onEventBroadcast.unsubscribe(assigner);
        packed.Test.Main.broadcastEvent("bar");
        assert.deepStrictEqual(result, "foo");
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
