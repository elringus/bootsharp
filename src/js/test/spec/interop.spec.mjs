import { describe, it, beforeAll, expect } from "vitest";
import { bootSideload, Test } from "../cs.mjs";

const TrackType = Test.Types.TrackType;

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", () => {
        expect(Test.invokeVoid).throw(/Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    beforeAll(bootSideload);
    it("throws when invoking un-assigned JS function from C#", () => {
        const error = /Failed to invoke '.+' from C#. Make sure to assign function in JavaScript/;
        Test.onMainInvoked = undefined;
        expect(Test.getBytes).toBeUndefined();
        expect(Test.getString).toBeUndefined();
        expect(Test.getStringAsync).toBeUndefined();
        expect(Test.throwJS).toBeUndefined();
        expect(Test.onMainInvoked).toBeUndefined();
        expect(Test.Types.getRegistry).toBeUndefined();
        expect(Test.Types.getRegistries).toBeUndefined();
        expect(Test.Types.getRegistryMap).toBeUndefined();
        expect(() => Test.getStringSerialized()).throw(error);
        expect(() => Test.getStringAsyncSerialized()).throw(error);
        expect(() => Test.getBytesSerialized()).throw(error);
        expect(() => Test.throwJSSerialized()).throw(error);
        expect(() => Test.onMainInvokedSerialized()).throw(error);
        expect(() => Test.Types.getRegistrySerialized()).throw(error);
        expect(() => Test.Types.getRegistriesSerialized()).throw(error);
        expect(() => Test.Types.getRegistryMapSerialized()).throw(error);
    });
    it("can invoke C# method", async () => {
        expect(Test.joinStrings("foo", "bar")).toStrictEqual("foobar");
    });
    it("can invoke async C# method", async () => {
        expect(await Test.joinStringsAsync("foo", "bar")).toStrictEqual("foobar");
    });
    it("can transfer strings", () => {
        Test.getString = () => "foo";
        expect(Test.echoString()).toStrictEqual("foo");
    });
    it("can transfer decimals", () => {
        expect(Test.sumDoubles(-1, 2.75)).toStrictEqual(1.75);
    });
    it("can transfer dates", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(Test.addDays(date, 7));
        expect(actual).toStrictEqual(expected);
    });
    it("can transfer byte array", () => {
        Test.getBytes = () => new Uint8Array([
            0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69, 0x6e,
            0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79,
            0x2c, 0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e,
            0x2e, 0x20, 0x4e, 0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20,
            0x66, 0x72, 0x65, 0x74, 0x2e
        ]);
        const echo = Test.echoBytes();
        expect(Test.bytesToString(echo)).toStrictEqual("Everything's shiny, Captain. Not to fret.");
    });
    it("can transfer structs", () => {
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
    it("can transfer lists as arrays", async () => {
        Test.Types.getRegistries = () => [{ wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }] }];
        const result = await Test.Types.concatRegistriesAsync([
            { wheeled: [{ id: "bar", maxSpeed: 1, wheelCount: 9 }] },
            { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }] }
        ]);
        expect(result).toStrictEqual([
            { wheeled: [{ id: "bar", maxSpeed: 1, wheelCount: 9 }] },
            { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }] },
            { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }] }
        ]);
    });
    it("can transfer dictionaries as maps", async () => {
        // ES6 Map doesn't natively support JSON serialization, so using plain objects.
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map
        Test.Types.getRegistryMap = () => ({
            foo: { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }] },
            bar: { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }] }
        });
        const result = await Test.Types.mapRegistriesAsync({
            baz: { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }] }
        });
        expect(result).toStrictEqual({
            baz: { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }] },
            foo: { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }] },
            bar: { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }] }
        });
    });
    it("can invoke assigned JS functions in C#", () => {
        Test.Types.getRegistry = () => ({ wheeled: [{ maxSpeed: 1 }], tracked: [{ maxSpeed: 2 }] });
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
    it("can invoke async method with async js callback", async () => {
        Test.getStringAsync = async () => {
            await new Promise(res => setTimeout(res, 100));
            return "foo";
        };
        expect(await Test.echoStringAsync()).toStrictEqual("foo");
    });
    it("maps enums by both indexes and strings", () => {
        expect(TrackType[0]).toStrictEqual("Rubber");
        expect(TrackType[1]).toStrictEqual("Chain");
        expect(TrackType[TrackType.Rubber]).toStrictEqual("Rubber");
        expect(TrackType[TrackType.Chain]).toStrictEqual("Chain");
    });
    it("can compare indexed enums", () => {
        expect(Test.getIdxEnumOne() === Test.IdxEnum.One).toBeTruthy();
        expect(Test.getIdxEnumOne() === Test.IdxEnum.Two).not.toBeTruthy();
    });
});
