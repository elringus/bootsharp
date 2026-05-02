import { describe, it, beforeAll, expect, vi } from "vitest";
import { Event, Test, bootSideload } from "../cs";

const TrackType = Test.Types.TrackType;

class Imported implements Test.Types.IImportedInstanced {
    constructor(private arg: string) { }
    record: Test.Types.Record = { id: "foo" };
    onRecordChanged = new Event<[Test.Types.IImportedInstanced, Test.Types.Record | undefined]>();
    getInstanceArg() { return this.arg; }
    async getRecordIdAsync(record: Test.Types.Record) {
        await new Promise(res => setTimeout(res, 1));
        return record.id;
    }
}

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", () => {
        expect(Test.Invokable.invokeVoid).throw(/Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    beforeAll(bootSideload);
    it("JS functions are unassigned by default", () => {
        expect(Test.Functions.jSFunction).toBeUndefined();
        expect(Test.Functions.getString).toBeUndefined();
        expect(Test.Functions.getStringAsync).toBeUndefined();
        expect(Test.Functions.getBytes).toBeUndefined();
        expect(Test.Platform.throwJS).toBeUndefined();
        expect(Test.Types.Registry.createVehicle).toBeUndefined();
        expect(Test.Types.RegistryProvider.getRegistry).toBeUndefined();
        expect(Test.Types.RegistryProvider.getRegistries).toBeUndefined();
        expect(Test.Types.RegistryProvider.getRegistryMap).toBeUndefined();
        expect(Test.Types.ImportedStatic.getInstanceAsync).toBeUndefined();
    });
    it("errs when invoking unassigned JS function", () => {
        expect(() => Test.Functions.invokeJSFunction())
            .throw(/Failed to invoke '.+' from C#. Make sure to assign the function in JavaScript/);
    });
    it("doesn't err when invoking assigned JS function", () => {
        Test.Functions.jSFunction = () => {};
        Test.Functions.invokeJSFunction();
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
                { id: "tank", trackType: TrackType.Chain, maxSpeed: Math.fround(20.005) },
                { id: "tractor", trackType: TrackType.Rubber, maxSpeed: Math.fround(15.9) }
            ]
        };
        const actual = Test.Types.Registry.echoRegistry(expected);
        expect(actual).toStrictEqual(expected);
    });
    it("empty string of a struct is transferred correctly", () => {
        const id = Test.Types.Registry.getVehicleWithEmptyId().id;
        expect(id).not.toBeNull();
        expect(id).not.toBeUndefined();
        expect(id).toStrictEqual("");
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
        Test.Types.RegistryProvider.getRegistryMap = () => new Map([
            ["foo", { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }],
            ["bar", { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }], tracked: [] }]
        ]);
        const result = await Test.Types.Registry.mapRegistriesAsync(new Map([
            ["baz", { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }]
        ]));
        expect(result).toStrictEqual(new Map([
            ["baz", { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }],
            ["foo", { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }],
            ["bar", { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }], tracked: [] }]
        ]));
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
    it("can invoke assigned JS functions from library assembly", () => {
        Test.Types.Registry.createVehicle = (id, maxSpeed) => ({ id, maxSpeed });
        expect(Test.Types.Registry.getVehicle("foo", 42)).toStrictEqual({ id: "foo", maxSpeed: 42 });
    });
    it("can subscribe to exported events", () => {
        const handler = vi.fn();
        Test.Event.onVehicleEvent.subscribe(handler);
        Test.Event.broadcastVehicleEvent(1, { id: "foo", maxSpeed: 50 }, TrackType.Rubber);
        expect(handler).toHaveBeenCalledWith(1, { id: "foo", maxSpeed: 50 }, TrackType.Rubber);
        Test.Event.broadcastVehicleEvent(255, undefined, TrackType.Chain);
        expect(handler).toHaveBeenCalledWith(255, undefined, TrackType.Chain);
    });
    it("can broadcast imported events", async () => {
        const handler = vi.fn();
        Test.Event.onImportedEventEchoed.subscribe(handler);

        const pending = Test.Event.echoImportedEventAsync();
        Test.Event.onImportedEvent.broadcast("imported");
        await pending;

        expect(handler).toHaveBeenCalledWith("imported");
        Test.Event.onImportedEventEchoed.unsubscribe(handler);
    });
    it("can subscribe to events from library assembly", () => {
        const handler = vi.fn();
        Test.Types.Registry.onVehicleBroadcast.subscribe(handler);
        Test.Types.Registry.broadcastVehicle({ id: "foo", maxSpeed: 42 });
        expect(handler).toHaveBeenCalledWith({ id: "foo", maxSpeed: 42 });
        Test.Types.Registry.broadcastVehicle(undefined);
        expect(handler).toHaveBeenCalledWith(undefined);
    });
    it("can un-subscribe from events", () => {
        const handler = vi.fn();
        Test.Event.onVehicleEvent.subscribe(handler);
        Test.Event.broadcastVehicleEvent(0, undefined, TrackType.Chain);
        Test.Event.onVehicleEvent.unsubscribe(handler);
        Test.Event.broadcastVehicleEvent(1, undefined, TrackType.Chain);
        expect(handler).toHaveBeenCalledWith(0, undefined, TrackType.Chain);
        expect(handler).not.toHaveBeenCalledWith(1, undefined, TrackType.Chain);
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
    it("can interop with imported modules", async () => {
        Test.Types.ImportedStatic.record = { id: "baz" };
        expect(Test.Types.Interfaces.getImportedStaticRecordIdAndSet({ id: "qux" })).toStrictEqual("baz");
        expect(Test.Types.ImportedStatic.record).toStrictEqual({ id: "qux" });
        Test.Types.ImportedStatic.record = undefined;
        expect(Test.Types.ImportedStatic.record).toBeUndefined();
        const handler = vi.fn();
        Test.Types.Interfaces.onImportedStaticRecordEchoed.subscribe(handler);
        let echo = Test.Types.Interfaces.echoImportedStaticRecordEventAsync();
        Test.Types.ImportedStatic.onRecordChanged.broadcast({ id: "static" });
        await echo;
        expect(handler).toHaveBeenCalledWith({ id: "static" });
        echo = Test.Types.Interfaces.echoImportedStaticRecordEventAsync();
        Test.Types.ImportedStatic.onRecordChanged.broadcast(undefined);
        await echo;
        expect(handler).toHaveBeenCalledWith(undefined);
        Test.Types.Interfaces.onImportedStaticRecordEchoed.unsubscribe(handler);
    });
    it("can interop with imported instances", async () => {
        Test.Types.ImportedStatic.getInstanceAsync = async (arg) => {
            await new Promise(res => setTimeout(res, 1));
            return new Imported(arg);
        };
        const result1 = await Test.Types.Interfaces.getImportedArgAndRecordIdAsync({ id: "foo" }, "bar");
        const result2 = await Test.Types.Interfaces.getImportedArgAndRecordIdAsync({ id: "baz" }, "nya");
        expect(result1).toStrictEqual("foobar");
        expect(result2).toStrictEqual("baznya");
        expect(await Test.Types.Interfaces.getImportedInstanceArgAndRecordIdAsync({ id: "zip" }, "qux"))
            .toStrictEqual("quxfoozip");
        const imported = new Imported("evt");
        const handler = vi.fn();
        Test.Types.Interfaces.onImportedInstanceRecordEchoed.subscribe(handler);
        let echo = Test.Types.Interfaces.echoImportedInstanceRecordEventAsync(imported);
        imported.onRecordChanged.broadcast(imported, { id: "instance" });
        await echo;
        expect(handler).toHaveBeenCalledWith("instance");
        echo = Test.Types.Interfaces.echoImportedInstanceRecordEventAsync(imported);
        imported.onRecordChanged.broadcast(imported, undefined);
        await echo;
        expect(handler).toHaveBeenCalledWith(undefined);
        Test.Types.Interfaces.onImportedInstanceRecordEchoed.unsubscribe(handler);
    });
    it("can interop with exported modules", () => {
        const record = { id: "foo" };
        const handler = vi.fn();
        Test.Types.ExportedStatic.onRecordChanged.subscribe(handler);
        Test.Types.ExportedStatic.record = record;
        expect(Test.Types.ExportedStatic.record).toStrictEqual(record);
        expect(handler).toHaveBeenCalledWith(record);
        Test.Types.ExportedStatic.record = { id: "bar" };
        expect(Test.Types.ExportedStatic.record).toStrictEqual({ id: "bar" });
        expect(handler).toHaveBeenCalledWith({ id: "bar" });
        Test.Types.ExportedStatic.record = undefined;
        expect(Test.Types.ExportedStatic.record).toBeUndefined();
        expect(handler).toHaveBeenCalledWith(undefined);
        Test.Types.ExportedStatic.onRecordChanged.unsubscribe(handler);
    });
    it("can interop with exported instances", async () => {
        const exported = await Test.Types.ExportedStatic.getInstanceAsync("bar");
        const handler = vi.fn();
        expect(exported.getInstanceArg()).toStrictEqual("bar");
        expect(await exported.getRecordIdAsync({ id: "foo" })).toStrictEqual("foo");
        expect(exported.record).toBeUndefined();
        exported.onRecordChanged.subscribe(handler);
        exported.record = { id: "qux" };
        expect(exported.record).toStrictEqual({ id: "qux" });
        expect(handler).toHaveBeenCalledWith(exported, { id: "qux" });
        exported.record = undefined;
        expect(exported.record).toBeUndefined();
        expect(handler).toHaveBeenCalledWith(exported, undefined);
        exported.onRecordChanged.unsubscribe(handler);
    });
    it("releases interface instances after use", async () => {
        Test.Types.ImportedStatic.getInstanceAsync = async (arg) => new Imported(arg);
        expect(await Test.Types.Interfaces.getImportedArgsAndFinalize("qux", "fox")).toStrictEqual(["qux", "fox"]);
        expect(await Test.Types.Interfaces.getImportedArgsAndFinalize("zip", "zap")).toStrictEqual(["zip", "zap"]);
    });
});
