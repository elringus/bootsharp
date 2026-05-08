import { describe, it, beforeAll, expect, vi } from "vitest";
import { Event, Test, bootRuntime } from "../cs";

const TrackType = Test.Library.TrackType;

class Imported implements Test.Library.IImportedInstanced {
    onRecordChanged = new Event<[Test.Library.IImportedInstanced, Test.Library.Record | undefined]>();
    record: Test.Library.Record | undefined = { id: "initial-rec" };
    inner = new ImportedInner();
    constructor(private arg: string) { }
    getInstanceArg() { return this.arg; }
    async getRecordIdAsync(record: Test.Library.Record) {
        await new Promise(res => setTimeout(res, 1));
        return record.id;
    }
}

class ImportedInner implements Test.Library.IImportedInnerInstanced {
    onCountChanged = new Event<[number]>();
    #count = 0;
    get count() { return this.#count; }
    set count(value) { this.onCountChanged.broadcast(this.#count = value); }
    increment() { this.count++; }
}

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", () => {
        expect(Test.Static.echoExported).throw(/Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    beforeAll(bootRuntime);
    it("JS functions are unassigned by default", () => {
        expect(Test.Platform.throwJS).toBeUndefined();
        expect(Test.Static.importedFunction).toBeUndefined();
        expect(Test.Static.echoImported).toBeUndefined();
        expect(Test.Static.echoImportedAsync).toBeUndefined();
        expect(Test.Library.Registries.createVehicle).toBeUndefined();
        expect(Test.Library.RegistryProvider.getRegistry).toBeUndefined();
        expect(Test.Library.RegistryProvider.getRegistries).toBeUndefined();
        expect(Test.Library.RegistryProvider.getRegistryMap).toBeUndefined();
        expect(Test.Library.ImportedModule.getInstanceAsync).toBeUndefined();
    });
    it("errs when invoking unassigned imported function", () => {
        expect(() => Test.Static.invokeImportedFunction())
            .throw(/Failed to invoke '.+' from C#. Make sure to assign the function in JavaScript/);
    });
    it("can invoke assigned imported function", () => {
        Test.Static.importedFunction = vi.fn();
        Test.Static.invokeImportedFunction();
        expect(Test.Static.importedFunction).toHaveBeenCalledOnce();
    });
    it("can interop with imported statics", async () => {
        let prop = "initial imported";
        Test.Static.importedProperty = { get: () => prop, set: v => prop = v };
        Test.Static.echoImported = bytes => bytes;
        Test.Static.echoImportedAsync = async bytes => {
            await new Promise(res => setTimeout(res, 1));
            return bytes;
        };
        const promise = Test.Static.canInteropWithImportedStaticsAsync();
        Test.Static.importedEvent.broadcast("event payload");
        await promise;
    });
    it("can interop with exported statics", async () => {
        expect(Test.Static.echoExported(new Uint8Array([2, 4])).reduce((s, i) => s + i, 0)).toStrictEqual(6);
        expect((await Test.Static.echoExportedAsync(new Uint8Array([4, 2]))).reduce((s, i) => s + i, 0)).toStrictEqual(6);
        expect(Test.Static.exportedProperty).toStrictEqual("initial exported");
        Test.Static.exportedProperty = "set";
        expect(Test.Static.exportedProperty).toStrictEqual("set");
        const handler = vi.fn();
        Test.Static.exportedEvent.subscribe(handler);
        Test.Static.broadcastExportedEvent("foo");
        expect(handler).toHaveBeenCalledWith("foo");
        Test.Static.broadcastExportedEvent(undefined);
        expect(handler).toHaveBeenCalledWith(undefined);
        Test.Static.exportedEvent.unsubscribe(handler);
        Test.Static.broadcastExportedEvent("bar");
        expect(handler).not.toHaveBeenCalledWith("bar");
    });
    it("can interop with imported modules", async () => {
        let record: Test.Library.Record | undefined = { id: "initial" };
        Test.Library.ImportedModule.record = { get: () => record, set: v => record = v };
        Test.Library.ImportedModule.getInstanceAsync = async (arg) => {
            await new Promise(res => setTimeout(res, 1));
            return new Imported(arg);
        };
        const promise = Test.Library.Modules.canInteropWithImportedModuleAsync();
        Test.Library.ImportedModule.onRecordChanged.broadcast({ id: "event-rec" });
        await promise;
    });
    it("can interop with exported modules", async () => {
        const handler = vi.fn();
        Test.Library.ExportedModule.onRecordChanged.subscribe(handler);
        Test.Library.ExportedModule.record = { id: "set" };
        expect(Test.Library.ExportedModule.record).toStrictEqual({ id: "set" });
        expect(handler).toHaveBeenCalledWith({ id: "set" });
        Test.Library.ExportedModule.record = undefined;
        expect(Test.Library.ExportedModule.record).toBeUndefined();
        expect(handler).toHaveBeenCalledWith(undefined);
        const inst = await Test.Library.ExportedModule.getInstanceAsync("module-arg");
        expect(inst.getInstanceArg()).toStrictEqual("module-arg");
        Test.Library.ExportedModule.onRecordChanged.unsubscribe(handler);
    });
    it("can interop with imported instances", async () => {
        Test.Library.ImportedModule.getInstanceAsync = async (arg) => new Imported(arg);
        const imported = new Imported("instance-arg");
        const promise = Test.Library.Modules.canInteropWithImportedInstanceAsync(imported);
        imported.onRecordChanged.broadcast(imported, { id: "event-rec" });
        await promise;
    });
    it("can interop with exported instances", async () => {
        const exported = await Test.Library.ExportedModule.getInstanceAsync("instance-arg");
        const handler = vi.fn();
        expect(exported.getInstanceArg()).toStrictEqual("instance-arg");
        expect(await exported.getRecordIdAsync({ id: "rec" })).toStrictEqual("rec");
        expect(exported.record).toBeUndefined();
        exported.onRecordChanged.subscribe(handler);
        exported.record = { id: "set" };
        expect(exported.record).toStrictEqual({ id: "set" });
        expect(handler).toHaveBeenCalledWith(exported, { id: "set" });
        exported.record = undefined;
        expect(exported.record).toBeUndefined();
        expect(handler).toHaveBeenCalledWith(exported, undefined);
        exported.onRecordChanged.unsubscribe(handler);
    });
    it("can interop with imported inner instances", () => {
        Test.Library.Modules.canInteropWithImportedInnerInstance(new Imported(""));
    });
    it("can interop with exported inner instances", async () => {
        const handler = vi.fn();
        const inner = (await Test.Library.ExportedModule.getInstanceAsync("bar")).inner;
        inner.onCountChanged.subscribe(handler);
        inner.count = 0;
        expect(handler).toHaveBeenCalledWith(0);
        inner.increment();
        expect(handler).toHaveBeenCalledWith(1);
        inner.increment();
        expect(inner.count).toStrictEqual(2);
    });
    it("releases instances after use", async () => {
        Test.Library.ImportedModule.getInstanceAsync = async (arg) => new Imported(arg);
        expect(await Test.Library.Modules.getImportedArgsAndFinalize("qux", "fox")).toStrictEqual(["qux", "fox"]);
        expect(await Test.Library.Modules.getImportedArgsAndFinalize("zip", "zap")).toStrictEqual(["zip", "zap"]);
    });
    it("can echo records", () => {
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
        const actual = Test.Library.Registries.echoRegistry(expected);
        expect(actual).toStrictEqual(expected);
    });
    it("empty string of a record is transferred correctly", () => {
        const id = Test.Library.Registries.getVehicleWithEmptyId().id;
        expect(id).not.toBeNull();
        expect(id).not.toBeUndefined();
        expect(id).toStrictEqual("");
    });
    it("can transfer lists as arrays", async () => {
        Test.Library.RegistryProvider.getRegistries = () => [{
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }],
            tracked: []
        }];
        const result = await Test.Library.Registries.concatRegistriesAsync([
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
        Test.Library.RegistryProvider.getRegistryMap = () => new Map([
            ["foo", { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }],
            ["bar", { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }], tracked: [] }]
        ]);
        const result = await Test.Library.Registries.mapRegistriesAsync(new Map([
            ["baz", { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }]
        ]));
        expect(result).toStrictEqual(new Map([
            ["baz", { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }],
            ["foo", { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }],
            ["bar", { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }], tracked: [] }]
        ]));
    });
    it("can invoke assigned JS functions in C#", () => {
        Test.Library.RegistryProvider.getRegistry = () => ({
            wheeled: [{ id: "", maxSpeed: 1, wheelCount: 0 }],
            tracked: [{ id: "", maxSpeed: 2, trackType: TrackType.Chain }]
        });
        expect(Test.Library.Registries.countTotalSpeed()).toStrictEqual(3);
    });
    it("can invoke assigned JS functions from a non-entry assembly", () => {
        Test.Library.Registries.createVehicle = (id, maxSpeed) => ({ id, maxSpeed });
        expect(Test.Library.Registries.getVehicle("foo", 42)).toStrictEqual({ id: "foo", maxSpeed: 42 });
    });
    it("can subscribe to events from a non-entry assembly", () => {
        const handler = vi.fn();
        Test.Library.Registries.onVehicleBroadcast.subscribe(handler);
        Test.Library.Registries.broadcastVehicle({ id: "foo", maxSpeed: 42 });
        expect(handler).toHaveBeenCalledWith({ id: "foo", maxSpeed: 42 });
        Test.Library.Registries.broadcastVehicle(undefined);
        expect(handler).toHaveBeenCalledWith(undefined);
    });
    it("can catch js exception", () => {
        Test.Platform.throwJS = function () { throw new Error("foo"); };
        expect(Test.Platform.catchException()!.split("\n")[0]).toStrictEqual("Error: foo");
    });
    it("can catch dotnet exceptions", () => {
        expect(() => Test.Platform.throwCS("bar")).throw("bar");
    });
    it("can catch dotnet exceptions from async methods", async () => {
        await expect(Test.Platform.throwCSAsync("baz")).rejects.toThrow("baz");
    });
    it("maps enums by both indexes and strings", () => {
        expect(Test.Static.Enum[1]).toStrictEqual("One");
        expect(Test.Static.Enum[2]).toStrictEqual("Two");
        expect(Test.Static.Enum[Test.Static.Enum.One]).toStrictEqual("One");
        expect(Test.Static.Enum[Test.Static.Enum.Two]).toStrictEqual("Two");
    });
    it("can compare indexed enums", () => {
        expect(Test.Static.getEnum(1) === Test.Static.Enum.One).toBeTruthy();
        expect(Test.Static.getEnum(0) === Test.Static.Enum.One).not.toBeTruthy();
    });
    it("can transfer dates", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(Test.Static.addDays(date, 7));
        expect(actual).toStrictEqual(expected);
    });
});
