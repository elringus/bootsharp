import { describe, it, beforeAll, expect } from "vitest";
import { Test, bootSideload, any, to } from "../cs";

const TrackType = Test.Types.TrackType;

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", () => {
        expect(Test.Invokable.invokeVoid).throw(/Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    beforeAll(bootSideload);
    it("throws when invoking un-assigned JS function from C#", () => {
        const error = /Failed to invoke '.+' from C#. Make sure to assign function in JavaScript/;
        any(Test.Program).onMainInvoked = undefined;
        expect(() => to<() => void>(Test.Program).onMainInvokedSerialized()).throw(error);
    });
    it("can invoke C# method", async () => {
        expect(Test.Invokable.joinStrings("foo", "bar")).toStrictEqual("foobar");
    });
    it("can invoke async C# method", async () => {
        expect(await Test.Invokable.joinStringsAsync("foo", "bar")).toStrictEqual("foobar");
    });
    it("can transfer strings", () => {
        Test.Functions.getString = () => "foo";
        expect(Test.Functions.echoString()).toStrictEqual("foo");
    });
    it("can transfer decimals", () => {
        expect(Test.Invokable.sumDoubles(-1, 2.75)).toStrictEqual(1.75);
    });
    it("can transfer dates", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(Test.Invokable.addDays(date, 7));
        expect(actual).toStrictEqual(expected);
    });
    it("can transfer byte array", () => {
        Test.Functions.getBytes = () => new Uint8Array([
            0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69, 0x6e,
            0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79,
            0x2c, 0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e,
            0x2e, 0x20, 0x4e, 0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20,
            0x66, 0x72, 0x65, 0x74, 0x2e
        ]);
        const echo = Test.Functions.echoBytes();
        expect(Test.Invokable.bytesToString(echo)).toStrictEqual("Everything's shiny, Captain. Not to fret.");
    });
    it("can transfer byte array async", async () => {
        expect(await Test.Functions.echoBytesAsync(new Uint8Array([
            0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69, 0x6e,
            0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79,
            0x2c, 0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e,
            0x2e, 0x20, 0x4e, 0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20,
            0x66, 0x72, 0x65, 0x74, 0x2e
        ]))).toStrictEqual(new Uint8Array([
            0x45, 0x76, 0x65, 0x72, 0x79, 0x74, 0x68, 0x69, 0x6e,
            0x67, 0x27, 0x73, 0x20, 0x73, 0x68, 0x69, 0x6e, 0x79,
            0x2c, 0x20, 0x43, 0x61, 0x70, 0x74, 0x61, 0x69, 0x6e,
            0x2e, 0x20, 0x4e, 0x6f, 0x74, 0x20, 0x74, 0x6f, 0x20,
            0x66, 0x72, 0x65, 0x74, 0x2e
        ]));
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
        const actual = Test.Types.Registry.echoRegistry(expected);
        expect(actual).toStrictEqual(expected);
    });
    it("can transfer lists as arrays", async () => {
        Test.Types.RegistryProvider.getRegistries = () => [{
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }],
            tracked: []
        }];
        const result = await Test.Types.Registry.concatRegistriesAsync([
            { wheeled: [{ id: "bar", maxSpeed: 1, wheelCount: 9 }], tracked: [] },
            { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }
        ]);
        expect(result).toStrictEqual([
            { wheeled: [{ id: "bar", maxSpeed: 1, wheelCount: 9 }], tracked: [] },
            { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] },
            { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }
        ]);
    });
    it("can transfer dictionaries as maps", async () => {
        // ES6 Map doesn't natively support JSON serialization, so using plain objects.
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Map
        Test.Types.RegistryProvider.getRegistryMap = () => (<never>{
            foo: { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }] },
            bar: { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }] }
        });
        const result = await Test.Types.Registry.mapRegistriesAsync(<never>{
            baz: { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }] }
        });
        expect(result).toStrictEqual({
            baz: { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }] },
            foo: { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }] },
            bar: { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }] }
        });
    });
    it("can transfer raw arrays", () => {
        expect(Test.Functions.echoStringArray(["foo", "bar"]))
            .toStrictEqual(["foo", "bar"]);
        expect(Test.Functions.echoDoubleArray(new Float64Array([0.5, -1.9])))
            .toStrictEqual(new Float64Array([0.5, -1.9]));
        expect(Test.Functions.echoIntArray(new Int32Array([1, 2])))
            .toStrictEqual(new Int32Array([1, 2]));
        expect(Test.Functions.echoByteArray(new Uint8Array([1, 2])))
            .toStrictEqual(new Uint8Array([1, 2]));
    });
    it("can transfer collection expressions", () => {
        expect(Test.Functions.echoColExprString(["foo", "bar"])).toStrictEqual(["foo", "bar"]);
        expect(Test.Functions.echoColExprDouble([0.5, -1.9])).toStrictEqual([0.5, -1.9]);
        expect(Test.Functions.echoColExprInt([1, 2])).toStrictEqual([1, 2]);
        expect(Test.Functions.echoColExprByte([1, 2])).toStrictEqual([1, 2]);
    });
    it("can invoke assigned JS functions in C#", () => {
        Test.Types.RegistryProvider.getRegistry = () => ({
            wheeled: [{ id: "", maxSpeed: 1, wheelCount: 0 }],
            tracked: [{ id: "", maxSpeed: 2, trackType: TrackType.Chain }]
        });
        expect(Test.Types.Registry.countTotalSpeed()).toStrictEqual(3);
    });
    it("can subscribe to events", () => {
        let eventArg1, multipleArg1, multipleArg2, multipleArg3;
        Test.Event.onEvent.subscribe(v => eventArg1 = v);
        Test.Event.onEventMultiple.subscribe((a1, a2, a3) => {
            multipleArg1 = a1;
            multipleArg2 = a2;
            multipleArg3 = a3;
        });
        Test.Event.broadcastEvent("foo");
        expect(eventArg1).toStrictEqual("foo");
        Test.Event.broadcastEventMultiple(1, { id: "foo", maxSpeed: 50 }, TrackType.Rubber);
        expect(multipleArg1).toStrictEqual(1);
        expect(multipleArg2).toStrictEqual({ id: "foo", maxSpeed: 50 });
        expect(multipleArg3).toStrictEqual(TrackType.Rubber);
        Test.Event.broadcastEventMultiple(255, <never>undefined, TrackType.Chain);
        expect(multipleArg1).toStrictEqual(255);
        expect(multipleArg2).toBeUndefined();
        expect(multipleArg3).toStrictEqual(TrackType.Chain);
    });
    it("can un-subscribe from events", () => {
        let result = "";
        const assigner = (v: string) => result = v;
        Test.Event.onEvent.subscribe(assigner);
        Test.Event.broadcastEvent("foo");
        Test.Event.onEvent.unsubscribe(assigner);
        Test.Event.broadcastEvent("bar");
        expect(result).toStrictEqual("foo");
    });
    it("can catch js exception", () => {
        Test.Platform.throwJS = function () { throw new Error("foo"); };
        expect(Test.Platform.catchException()!.split("\n")[0]).toStrictEqual("Error: foo");
    });
    it("can catch dotnet exceptions", () => {
        expect(() => Test.Platform.throwCS("bar")).throw("bar");
    });
    it("can invoke async method with async js callback", async () => {
        Test.Functions.getStringAsync = async () => {
            await new Promise(res => setTimeout(res, 1));
            return "foo";
        };
        expect(await Test.Functions.echoStringAsync()).toStrictEqual("foo");
    });
    it("maps enums by both indexes and strings", () => {
        expect(TrackType[0]).toStrictEqual("Rubber");
        expect(TrackType[1]).toStrictEqual("Chain");
        expect(TrackType[TrackType.Rubber]).toStrictEqual("Rubber");
        expect(TrackType[TrackType.Chain]).toStrictEqual("Chain");
    });
    it("can compare indexed enums", () => {
        expect(Test.Invokable.getIdxEnumOne() === Test.IdxEnum.One).toBeTruthy();
        expect(Test.Invokable.getIdxEnumOne() === Test.IdxEnum.Two).not.toBeTruthy();
    });
    it("can interop with exported interfaces", async () => {
        const result = await Test.Program.getExportedArgAndVehicleIdAsync({ id: "foo", maxSpeed: 0 }, "bar");
        expect(result).toStrictEqual("foobar");
    });
    it("can interop with imported interfaces", async () => {
        class Imported {
            constructor(private arg: string) { }
            getInstanceArg() { return this.arg; }
            async getVehicleIdAsync(vehicle: Test.Types.Vehicle) {
                await new Promise(res => setTimeout(res, 1));
                return vehicle.id;
            }
        }
        Test.Types.ImportedStatic.getInstanceAsync = async (arg) => {
            await new Promise(res => setTimeout(res, 1));
            return new Imported(arg);
        };
        const result1 = await Test.Program.getImportedArgAndVehicleIdAsync({ id: "foo", maxSpeed: 0 }, "bar");
        const result2 = await Test.Program.getImportedArgAndVehicleIdAsync({ id: "baz", maxSpeed: 0 }, "nya");
        expect(result1).toStrictEqual("foobar");
        expect(result2).toStrictEqual("baznya");
    });
    it("empty string of a struct is transferred correctly", () => {
        const id = Test.Types.Registry.getWithEmptyId().id;
        expect(id).not.toBeNull();
        expect(id).not.toBeUndefined();
        expect(id).toStrictEqual("");
    });
});
