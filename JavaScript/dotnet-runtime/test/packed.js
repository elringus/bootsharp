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
        assert.throws(() => packed.Test.Main.TestEchoFunction([]), /.*JSException/);
        await assert.rejects(() => packed.Test.Main.TestAsyncEchoFunction([]), /.*JSException/);
        assert.throws(() => packed.Test.Types.CountTotalSpeed(), /.*JSException/);
    });
    it("can implement functions declared in C#", async () => {
        packed.Test.Main.EchoFunction = values => values;
        packed.Test.Main.AsyncEchoFunction = async values => values;
        packed.Test.Types.GetRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
        assert.deepStrictEqual(packed.Test.Main.TestEchoFunction(["a", "b"]), ["a", "b"]);
        assert.deepStrictEqual(await packed.Test.Main.TestAsyncEchoFunction(["c", "d"]), ["c", "d"]);
        assert.deepStrictEqual(packed.Test.Types.CountTotalSpeed(), 3);
    });
    it("can subscribe to events declared in C#", async () => {
        let result = "";
        packed.Test.Main.OnEventBroadcast.subscribe(v => result = v);
        packed.Test.Main.BroadcastEvent("foo");
        assert.deepStrictEqual(result, "foo");
    });
    it("can un-subscribe from events declared in C#", async () => {
        let result = "";
        const assigner = v => result = v;
        packed.Test.Main.OnEventBroadcast.subscribe(assigner);
        packed.Test.Main.BroadcastEvent("foo");
        packed.Test.Main.OnEventBroadcast.unsubscribe(assigner);
        packed.Test.Main.BroadcastEvent("bar");
        assert.deepStrictEqual(result, "foo");
    });
    it("handles events with array args properly", () => {
        let result = [];
        const assigner = v => result = v;
        packed.Test.Main.OnStringsBroadcast.subscribe(assigner);
        packed.Test.Main.BroadcastStrings(["foo", "bar"]);
        assert.deepStrictEqual(result, ["foo", "bar"]);
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
