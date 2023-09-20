// const assert = require("node:assert");
// const { describe, it, after } = require("bun:test");
// const { bootsharp, getDeclarations, getGeneratedMap } = require("./cs");
//
// describe("packed library", () => {
//     after(bootsharp.terminate);
//     it("can boot without specifying boot data", async () => {
//         await assert.doesNotReject(bootsharp.boot);
//         assert.deepStrictEqual(bootsharp.getBootStatus(), bootsharp.BootStatus.Booted);
//     });
//     it("re-exports dotnet members", async () => {
//         assert(bootsharp.BootStatus instanceof Object);
//         assert(bootsharp.getBootStatus instanceof Function);
//         assert(bootsharp.terminate instanceof Function);
//         assert(bootsharp.invoke instanceof Function);
//         assert(bootsharp.invokeAsync instanceof Function);
//         assert(bootsharp.createObjectReference instanceof Function);
//         assert(bootsharp.disposeObjectReference instanceof Function);
//         assert(bootsharp.createStreamReference instanceof Function);
//     });
//     it("exports C# methods grouped under assembly object", async () => {
//         assert.deepStrictEqual(bootsharp.Test.Main.joinStrings("a", "b"), "ab");
//         assert.deepStrictEqual(await bootsharp.Test.Main.joinStringsAsync("c", "d"), "cd");
//     });
//     it("when function is not implemented error is thrown", async () => {
//         assert.throws(() => bootsharp.Test.Main.testEchoFunction(""), /.*JSException/);
//         await assert.rejects(() => bootsharp.Test.Main.testAsyncEchoFunction(""), /.*JSException/);
//         assert.throws(() => bootsharp.Test.Types.countTotalSpeed(), /.*JSException/);
//     });
//     it("can implement functions declared in C#", async () => {
//         bootsharp.Test.Main.echoFunction = value => value;
//         bootsharp.Test.Main.asyncEchoFunction = async value => value;
//         bootsharp.Test.Types.getRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
//         assert.deepStrictEqual(bootsharp.Test.Main.testEchoFunction("a"), "a");
//         assert.deepStrictEqual(await bootsharp.Test.Main.testAsyncEchoFunction("b"), "b");
//         assert.deepStrictEqual(bootsharp.Test.Types.countTotalSpeed(), 3);
//     });
//     it("array args handled correctly", async () => {
//         bootsharp.Test.Main.arrayArgFunction = values => values;
//         assert.deepStrictEqual(bootsharp.Test.Main.testArrayArgFunction(["a", "b"]), ["a", "b"]);
//     });
//     it("can subscribe to events declared in C#", async () => {
//         let result = "";
//         bootsharp.Test.Main.onEventBroadcast.subscribe(v => result = v);
//         bootsharp.Test.Main.broadcastEvent("foo");
//         assert.deepStrictEqual(result, "foo");
//     });
//     it("can un-subscribe from events declared in C#", async () => {
//         let result = "";
//         const assigner = v => result = v;
//         bootsharp.Test.Main.onEventBroadcast.subscribe(assigner);
//         bootsharp.Test.Main.broadcastEvent("foo");
//         bootsharp.Test.Main.onEventBroadcast.unsubscribe(assigner);
//         bootsharp.Test.Main.broadcastEvent("bar");
//         assert.deepStrictEqual(result, "foo");
//     });
//     it("still can interop via strings", async () => {
//         assert.deepStrictEqual(bootsharp.invoke("Test.Main", "JoinStrings", "a", "b"), "ab");
//         assert.deepStrictEqual(await bootsharp.invokeAsync("Test.Main", "JoinStringsAsync", "a", "b"), "ab");
//     });
//     it("generates type definitions", () => {
//         assert(getDeclarations());
//     });
//     it("generates source map", () => {
//         assert(getGeneratedMap());
//     });
// });
