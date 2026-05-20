import { describe, it, beforeAll, expect, vi } from "vitest";
import { Event, bootRuntime } from "../cs";
import { Platform, Static } from "../cs/Test/bin/bootsharp/generated/modules/test.g.mjs";
import { IExportedModule, IImportedModule, Modules, Registries, IRegistryProvider, TrackType } from "../cs/Test/bin/bootsharp/generated/modules/test/library.g.mjs";
import type { IBidirectional, IImportedInstanced, IImportedInnerInstanced, Record } from "../cs/Test/bin/bootsharp/generated/modules/test/library.g.mjs";

class Imported implements IImportedInstanced {
    onRecordChanged = new Event<[IImportedInstanced, Record | undefined]>();
    record: Record | undefined = { id: "initial-rec" };
    inner = new ImportedInner();
    constructor(private arg: string) { }
    getInstanceArg() { return this.arg; }
    async getRecordIdAsync(record: Record) {
        await new Promise(res => setTimeout(res, 1));
        return record.id;
    }
}

class ImportedInner implements IImportedInnerInstanced {
    onCountChanged = new Event<[number]>();
    #count = 0;
    get count() { return this.#count; }
    set count(value) { this.onCountChanged.broadcast(this.#count = value); }
    increment() { this.count++; }
}

class BidirectionalJS implements IBidirectional {
    onBiChanged = new Event<[IBidirectional]>();
    #bi: IBidirectional;
    constructor() { this.#bi = this; }
    get bi() { return this.#bi; }
    set bi(value) { this.onBiChanged.broadcast(this.#bi = value); }
    echoBi(bi: IBidirectional) { return bi; }
}

describe("while bootsharp is not booted", () => {
    it("throws when attempting to invoke C# APIs", () => {
        expect(Static.echoExported).throw(/Boot the runtime before invoking C# APIs/);
    });
});

describe("while bootsharp is booted", () => {
    beforeAll(bootRuntime);
    it("JS functions are unassigned by default", () => {
        expect(Platform.throwJS).toBeUndefined();
        expect(Static.importedFunction).toBeUndefined();
        expect(Static.echoImported).toBeUndefined();
        expect(Static.echoImportedAsync).toBeUndefined();
        expect(Registries.createVehicle).toBeUndefined();
        expect(IRegistryProvider.getRegistry).toBeUndefined();
        expect(IRegistryProvider.getRegistries).toBeUndefined();
        expect(IRegistryProvider.getRegistryMap).toBeUndefined();
        expect(IImportedModule.getInstanceAsync).toBeUndefined();
    });
    // NativeAOT-LLVM's runtime wraps C# exceptions thrown to JS in a generic
    // `new Error("C# exception from NativeAOT")` and discards the original
    // JSException.Message. Re-enable once the runtime preserves the message or
    // we add a DotNetPatcher patch that extracts it.
    it.skip("errs when invoking unassigned imported function", () => {
        expect(() => Static.invokeImportedFunction())
            .throw(/Failed to invoke '.+' from C#. Make sure to assign the function in JavaScript/);
    });
    it("can invoke assigned imported function", () => {
        Static.importedFunction = vi.fn();
        Static.invokeImportedFunction();
        expect(Static.importedFunction).toHaveBeenCalledOnce();
    });
    it("can interop with imported statics", async () => {
        let prop = "initial imported";
        Static.importedProperty = { get: () => prop, set: v => prop = v };
        Static.echoImported = bytes => bytes;
        Static.echoImportedAsync = async bytes => {
            await new Promise(res => setTimeout(res, 1));
            return bytes;
        };
        const promise = Static.canInteropWithImportedStaticsAsync();
        Static.importedEvent.broadcast("event payload");
        await promise;
    });
    it("can interop with exported statics", async () => {
        expect(Static.echoExported(new Uint8Array([2, 4])).reduce((s, i) => s + i, 0)).toStrictEqual(6);
        expect((await Static.echoExportedAsync(new Uint8Array([4, 2]))).reduce((s, i) => s + i, 0)).toStrictEqual(6);
        expect(Static.exportedProperty).toStrictEqual("initial exported");
        Static.exportedProperty = "set";
        expect(Static.exportedProperty).toStrictEqual("set");
        const handler = vi.fn();
        Static.exportedEvent.subscribe(handler);
        Static.broadcastExportedEvent("foo");
        expect(handler).toHaveBeenCalledWith("foo");
        Static.broadcastExportedEvent(undefined);
        expect(handler).toHaveBeenCalledWith(undefined);
        Static.exportedEvent.unsubscribe(handler);
        Static.broadcastExportedEvent("bar");
        expect(handler).not.toHaveBeenCalledWith("bar");
    });
    it("can interop with imported modules", async () => {
        let record: Record | undefined = { id: "initial" };
        IImportedModule.record = { get: () => record, set: v => record = v };
        IImportedModule.getInstanceAsync = async (arg) => {
            await new Promise(res => setTimeout(res, 1));
            return new Imported(arg);
        };
        const promise = Modules.canInteropWithImportedModuleAsync();
        IImportedModule.onRecordChanged.broadcast({ id: "event-rec" });
        await promise;
    });
    it("can interop with exported modules", async () => {
        const handler = vi.fn();
        IExportedModule.onRecordChanged.subscribe(handler);
        IExportedModule.record = { id: "set" };
        expect(IExportedModule.record).toStrictEqual({ id: "set" });
        expect(handler).toHaveBeenCalledWith({ id: "set" });
        IExportedModule.record = undefined;
        expect(IExportedModule.record).toBeUndefined();
        expect(handler).toHaveBeenCalledWith(undefined);
        const inst = await IExportedModule.getInstanceAsync("module-arg");
        expect(inst.getInstanceArg()).toStrictEqual("module-arg");
        IExportedModule.onRecordChanged.unsubscribe(handler);
    });
    it("can interop with imported instances", async () => {
        IImportedModule.getInstanceAsync = async (arg) => new Imported(arg);
        const imported = new Imported("instance-arg");
        const promise = Modules.canInteropWithImportedInstanceAsync(imported);
        imported.onRecordChanged.broadcast(imported, { id: "event-rec" });
        await promise;
    });
    it("can interop with exported instances", async () => {
        const exported = await IExportedModule.getInstanceAsync("instance-arg");
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
        Modules.canInteropWithImportedInnerInstance(new Imported(""));
    });
    it("can interop with bidirectional instances", () => {
        const factory = () => new BidirectionalJS();
        Modules.importBi = factory;
        expect(Modules.importBi).toBe(factory);
        const exp = Modules.exportBi();
        const js = new BidirectionalJS();
        const handler = vi.fn();
        exp.onBiChanged.subscribe(handler);
        expect(exp.echoBi(exp)).toBe(exp);
        expect(exp.echoBi(js)).toBe(js);
        exp.bi = js;
        expect(handler).toHaveBeenCalledWith(js);
        expect(exp.bi).toBe(js);
        exp.bi = exp;
        expect(handler).toHaveBeenCalledWith(exp);
        expect(exp.bi).toBe(exp);
        exp.onBiChanged.unsubscribe(handler);
        Modules.canInteropWithBidirectional();
    });
    it("can interop with exported inner instances", async () => {
        const handler = vi.fn();
        const inner = (await IExportedModule.getInstanceAsync("bar")).inner;
        inner.onCountChanged.subscribe(handler);
        inner.count = 0;
        expect(handler).toHaveBeenCalledWith(0);
        inner.increment();
        expect(handler).toHaveBeenCalledWith(1);
        inner.increment();
        expect(inner.count).toStrictEqual(2);
    });
    it("releases instances after use", async () => {
        IImportedModule.getInstanceAsync = async (arg) => new Imported(arg);
        expect(await Modules.getImportedArgsAndFinalize("qux", "fox")).toStrictEqual(["qux", "fox"]);
        expect(await Modules.getImportedArgsAndFinalize("zip", "zap")).toStrictEqual(["zip", "zap"]);
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
        const actual = Registries.echoRegistry(expected);
        expect(actual).toStrictEqual(expected);
    });
    it("empty string of a record is transferred correctly", () => {
        const id = Registries.getVehicleWithEmptyId().id;
        expect(id).not.toBeNull();
        expect(id).not.toBeUndefined();
        expect(id).toStrictEqual("");
    });
    it("can transfer lists as arrays", async () => {
        IRegistryProvider.getRegistries = () => [{
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }],
            tracked: []
        }];
        const result = await Registries.concatRegistriesAsync([
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
        IRegistryProvider.getRegistryMap = () => new Map([
            ["foo", { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }],
            ["bar", { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }], tracked: [] }]
        ]);
        const result = await Registries.mapRegistriesAsync(new Map([
            ["baz", { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }]
        ]));
        expect(result).toStrictEqual(new Map([
            ["baz", { tracked: [{ id: "baz", maxSpeed: 5, trackType: TrackType.Rubber }], wheeled: [] }],
            ["foo", { wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 0 }], tracked: [] }],
            ["bar", { wheeled: [{ id: "bar", maxSpeed: 15, wheelCount: 5 }], tracked: [] }]
        ]));
    });
    it("can invoke assigned JS functions in C#", () => {
        IRegistryProvider.getRegistry = () => ({
            wheeled: [{ id: "", maxSpeed: 1, wheelCount: 0 }],
            tracked: [{ id: "", maxSpeed: 2, trackType: TrackType.Chain }]
        });
        expect(Registries.countTotalSpeed()).toStrictEqual(3);
    });
    it("can invoke assigned JS functions from a non-entry assembly", () => {
        Registries.createVehicle = (id, maxSpeed) => ({ id, maxSpeed });
        expect(Registries.getVehicle("foo", 42)).toStrictEqual({ id: "foo", maxSpeed: 42 });
    });
    it("can subscribe to events from a non-entry assembly", () => {
        const handler = vi.fn();
        Registries.onVehicleBroadcast.subscribe(handler);
        Registries.broadcastVehicle({ id: "foo", maxSpeed: 42 });
        expect(handler).toHaveBeenCalledWith({ id: "foo", maxSpeed: 42 });
        Registries.broadcastVehicle(undefined);
        expect(handler).toHaveBeenCalledWith(undefined);
    });
    it("can interop with cs registry", () => {
        const reg = Registries.makeRegistry();
        const wheeled = [{ id: "car", maxSpeed: 100, wheelCount: 4 }];
        const tracked = [{ id: "tank", maxSpeed: 20, trackType: TrackType.Chain }];
        reg.wheeled = wheeled;
        reg.tracked = tracked;
        expect(reg.wheeled).toStrictEqual(wheeled);
        expect(reg.tracked).toStrictEqual(tracked);
    });
    // Raw C-ABI calls don't bridge JS exceptions back into C# catch blocks: a `throw`
    // inside a [DllImport] handler propagates as a host exception that bypasses the
    // C# try/catch. Same limitation in the opposite direction is captured below.
    it.skip("can catch js exception", () => {
        Platform.throwJS = function () { throw new Error("foo"); };
        expect(Platform.catchException()!.split("\n")[0]).toStrictEqual("Error: foo");
    });
    // Same NativeAOT-LLVM limitation as "errs when invoking unassigned imported
    // function": sync C# → JS exception flow loses the original message.
    it.skip("can catch dotnet exceptions", () => {
        expect(() => Platform.throwCS("bar")).throw("bar");
    });
    it("can catch dotnet exceptions from async methods", async () => {
        await expect(Platform.throwCSAsync("baz")).rejects.toThrow("baz");
    });
    it("maps enums by both indexes and strings", () => {
        expect(Static.Enum[1]).toStrictEqual("One");
        expect(Static.Enum[2]).toStrictEqual("Two");
        expect(Static.Enum[Static.Enum.One]).toStrictEqual("One");
        expect(Static.Enum[Static.Enum.Two]).toStrictEqual("Two");
    });
    it("can compare indexed enums", () => {
        expect(Static.getEnum(1) === Static.Enum.One).toBeTruthy();
        expect(Static.getEnum(0) === Static.Enum.One).not.toBeTruthy();
    });
    it("can transfer dates", () => {
        const date = new Date(1977, 3, 2);
        const expected = new Date(1977, 3, 9);
        const actual = new Date(Static.addDays(date, 7));
        expect(actual).toStrictEqual(expected);
    });
});
