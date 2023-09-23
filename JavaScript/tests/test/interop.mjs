import { throws, strictEqual, deepStrictEqual } from "node:assert";
import { describe, it, before, after } from "node:test";
import { boot, exit, Test } from "../cs.mjs";

const TrackType = Test.Types.TrackType;

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", async () => {
        throws(Test.invokeVoid, /Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    before(boot);
    after(exit);
    it("can send and receive string", () => {
        strictEqual(Test.joinStrings("foo", "bar"), "foobar");
    });
    it("can send and receive number", () => {
        strictEqual(Test.sumDoubles(-1, 2.75), 1.75);
    });
    it("can send and receive date", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(Test.addDays(date, 7));
        deepStrictEqual(actual, expected);
    });
    it("can send and receive custom data type", () => {
        const expected = {
            wheeled: [
                { id: "car", wheelCount: 4, maxSpeed: 100.0 },
                { id: "bicycle", wheelCount: 2, maxSpeed: 30.5 }
            ],
            tracked: [
                { id: "tank", trackType: TrackType.Chain, maxSpeed: 20.005 },
                { id: "tractor", trackType: TrackType.Rubber, maxSpeed: 15.9 }
            ]
        };
        const actual = Test.Types.echoRegistry(expected);
        deepStrictEqual(actual, expected);
    });
    it("throws when attempting to invoke un-assigned JS function", async () => {
        const error = /Failed to invoke 'Test\.(async)?[E|e]choFunction' JavaScript function: undefined/;
        throws(() => Test.testEchoFunction(""), error);
        throws(() => Test.testAsyncEchoFunction(""), error);
    });
    it("can invoke assigned JS functions in C#", async () => {
        Test.echoFunction = value => value;
        Test.Types.getRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
        deepStrictEqual(Test.testEchoFunction("a"), "a");
        strictEqual(Test.Types.countTotalSpeed(), 3);
    });
    it("can transfer array of strings", async () => {
        Test.arrayArgFunction = values => values;
        deepStrictEqual(Test.testArrayArgFunction(["a", "b"]), ["a", "b"]);
    });
    it("can subscribe to events", async () => {
        let result = "";
        Test.onEventBroadcast.subscribe(v => result = v);
        Test.broadcastEvent("foo");
        strictEqual(result, "foo");
    });
    it("can un-subscribe from events", async () => {
        let result = "";
        const assigner = v => result = v;
        Test.onEventBroadcast.subscribe(assigner);
        Test.broadcastEvent("foo");
        Test.onEventBroadcast.unsubscribe(assigner);
        Test.broadcastEvent("bar");
        strictEqual(result, "foo");
    });
    it("can transfer and decode raw bytes", () => {
        const bytes = new Uint8Array([0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
            0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
            0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
            0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e]);
        strictEqual(Test.bytesToString(bytes), "Everything's shiny, Captain. Not to fret.");
    });
    it("can catch js exception", () => {
        Test.throwJS = function () { throw new Error("foo"); };
        strictEqual(Test.catchException().split("\n")[0], "Error: foo");
    });
    it("can catch dotnet exceptions", () => {
        throws(() => Test.throwCS("bar"), /Error: bar/);
    });
});
