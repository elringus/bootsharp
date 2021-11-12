const assert = require("assert");
const { invoke, invokeAsync, terminate } = require("../dist/dotnet");
const { bootExample } = require("../example/example");

describe("interop", () => {
    before(bootExample);
    after(terminate);
    it("throws when assembly is not found", () => {
        assert.throws(() => invoke("Foo", "JoinStrings"), /.*no loaded assembly.*'Foo'/);
    });
    it("throws when method is not found", () => {
        assert.throws(() => invoke("Example", "Bar"), /.*does not contain.*"Bar"/);
    });
    it("can send and receive string", () => {
        assert.deepStrictEqual(invoke("Example", "JoinStrings", "foo", "bar"), "foobar");
    });
    it("can send and receive number", () => {
        assert.deepStrictEqual(invoke("Example", "SumDoubles", -1, 2.75), 1.75);
    });
    it("can send and receive object", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(invoke("Example", "AddDays", date, 7));
        assert.deepStrictEqual(actual, expected);
    });
    it("can invoke js function from dotnet", () => {
        let invokedFromDotNet = false;
        global.invokeFromDotNet = () => invokedFromDotNet = true;
        invoke("Example", "InvokeJS", "invokeFromDotNet");
        assert(invokedFromDotNet);
    });
    it("can process array with a js callback", () => {
        const array = ["a", "b", "c"];
        const expected = ["aa", "bb", "cc"];
        global.repeat = item => item + item;
        const actual = invoke("Example", "ForEachJS", array, "repeat");
        assert.deepStrictEqual(actual, expected);
    });
    it("can interop with async methods", async () => {
        assert.deepStrictEqual(await invokeAsync("Example", "JoinStringsAsync", "a", "b"), "ab");
    });
    it("can find method by alias", () => {
        assert.deepStrictEqual(invoke("Example", "EchoAlias", "foo"), "foo");
    });
    it("can interop with instance", () => {
        const instance = invoke("Example", "CreateInstance");
        assert.doesNotThrow(() => instance.invokeMethod("SetVar", "foo"));
        assert.deepStrictEqual(instance.invokeMethod("GetVar"), "foo");
        assert.doesNotThrow(() => instance.dispose());
    });
    it("can send instance to dotnet", () => {
        const instance1 = invoke("Example", "CreateInstance");
        const instance2 = invoke("Example", "CreateInstance");
        instance1.invokeMethod("SetVar", "bar");
        instance2.invokeMethod("SetFromOther", instance1);
        assert.deepStrictEqual(instance2.invokeMethod("GetVar"), "bar");
        instance1.dispose();
        instance2.dispose();
    });
    it("can send and receive raw bytes", () => {
        global.receiveBytes = bytes => new TextDecoder().decode(bytes);
        const bytes = new Uint8Array([0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
            0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
            0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
            0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e]);
        const expected = "Everything's shiny, Captain. Not to fret.";
        assert.deepStrictEqual(invoke("Example", "ReceiveBytes", bytes), expected);
        assert.deepStrictEqual(invoke("Example", "SendBytes"), expected);
    });
    it("can catch js exceptions", () => {
        global.throw = function () {
            throw new Error("foo");
        };
        assert.deepStrictEqual(invoke("Example", "CatchException").split("\n")[0], "foo");
    });
    it("can catch dotnet exceptions", () => {
        assert.throws(() => invoke("Example", "Throw", "bar"), /Error: System.Exception: bar/);
    });
    // TODO: Figure how to test async streaming without blocking the thread.
    // it("can stream", async () => {
    //     let received, resolve;
    //     global.sendStream = () => new Uint8Array(10000000);
    //     global.receiveStream = async ref => {
    //         received = await ref.arrayBuffer();
    //         resolve();
    //     };
    //     invoke("Example", "StartStream");
    //     await new Promise(r => resolve = r);
    //     assert.deepStrictEqual(received.length, 10000000);
    // });
    // TODO: Test unmarshalled interop.
    // https://docs.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet#unmarshalled-javascript-interop
});
