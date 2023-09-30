import { describe, it, beforeAll, expect } from "vitest";
import { boot, Test } from "../cs.mjs";

const TrackType = Test.Types.TrackType;

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", () => {
        expect(Test.invokeVoid).throw(/Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    beforeAll(boot);
    it("throws when attempting to invoke un-assigned JS function", () => {
        const error = /Failed to invoke '.+' JavaScript function: undefined/;
        Test._onMainInvoked = undefined;
        expect(() => Test.testEchoFunction("")).throw(error);
        expect(() => Test.arrayArgFunction()).throw(error);
        expect(() => Test.throwJS()).throw(error);
        expect(() => Test.onMainInvoked()).throw(error);
        expect(() => Test.Types.getRegistry()).throw(error);
    });
    it("can invoke C# method", async () => {
        expect(Test.joinStrings("foo", "bar")).toStrictEqual("foobar");
    });
    // it("can invoke async C# method", async () => {
    //     // TODO: Async failing in node https://github.com/dotnet/runtime/issues/92713
    //     expect(await Test.joinStringsAsync("foo", "bar")).toStrictEqual("foobar");
    // });
    it("can transfer decimals", () => {
        expect(Test.sumDoubles(-1, 2.75)).toStrictEqual(1.75);
    });
    it("can transfer dates", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(Test.addDays(date, 7));
        expect(actual).toStrictEqual(expected);
    });
    it("can transfer arrays", () => {
        Test.arrayArgFunction = values => values;
        expect(Test.testArrayArgFunction(["a", "b"])).toStrictEqual(["a", "b"]);
    });
    it("can transfer raw bytes", () => {
        const bytes = new Uint8Array([0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69,
            0x6e, 0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79, 0x2c,
            0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e, 0x2e, 0x20, 0x4e,
            0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20, 0x66, 0x72, 0x65, 0x74, 0x2e]);
        expect(Test.bytesToString(bytes)).toStrictEqual("Everything's shiny, Captain. Not to fret.");
    });
    it("can transfer structs", () => {
        // TODO: Test async transfer structs to check serialization with await.
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
        expect(actual).toStrictEqual(expected);
    });
    it("can invoke assigned JS functions in C#", () => {
        Test.echoFunction = value => value;
        Test.Types.getRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
        expect(Test.testEchoFunction("a")).toStrictEqual("a");
        expect(Test.Types.countTotalSpeed()).toStrictEqual(3);
    });
    it("can subscribe to events", () => {
        let eventArg1, multipleArg1, multipleArg2, multipleArg3;
        Test.onEvent.subscribe(v => eventArg1 = v);
        Test.onEventMultiple.subscribe((a1, a2, a3) => {
            multipleArg1 = a1;
            multipleArg2 = a2;
            multipleArg3 = a3;
        });
        Test.broadcastEvent("foo");
        Test.broadcastEventMultiple(1, { id: "foo", maxSpeed: 50 }, TrackType.Rubber);
        expect(multipleArg1).toStrictEqual(1);
        expect(multipleArg2).toStrictEqual({ id: "foo", maxSpeed: 50 });
        expect(multipleArg3).toStrictEqual(TrackType.Rubber);
        Test.broadcastEventMultiple(255, undefined, TrackType.Chain);
        expect(multipleArg1).toStrictEqual(255);
        expect(multipleArg2).toBeUndefined();
        expect(multipleArg3).toStrictEqual(TrackType.Chain);
    });
    it("can un-subscribe from events", () => {
        let result = "";
        const assigner = v => result = v;
        Test.onEvent.subscribe(assigner);
        Test.broadcastEvent("foo");
        Test.onEvent.unsubscribe(assigner);
        Test.broadcastEvent("bar");
        expect(result).toStrictEqual("foo");
    });
    it("can catch js exception", () => {
        Test.throwJS = function () { throw new Error("foo"); };
        expect(Test.catchException().split("\n")[0]).toStrictEqual("Error: foo");
    });
    it("can catch dotnet exceptions", () => {
        expect(() => Test.throwCS("bar")).throw("bar");
    });
});
