import { describe, expect, it, beforeAll } from "vitest";
import { Event, Collection, List, Dictionary, CancellationToken, bootRuntime } from "../cs";
import { BCL } from "../cs/Test/bin/bootsharp/generated/modules/test.g.mjs";

describe("BCL", () => {
    beforeAll(bootRuntime);
    describe("event", () => {
        it("can broadcast without subscribers", () => {
            new Event().broadcast();
        });
        it("doesn't mind unsubscribing null handler", () => {
            new Event().unsubscribe(<never>null);
        });
        it("warns when unsubscribing handler which is not subscribed", () => {
            let warning;
            new Event({ warn: msg => warning = msg }).unsubscribe(<never>it);
            expect(warning).include("handler is not subscribed");
        });
        it("warns when subscribing handler which is already subscribed", () => {
            let warning;
            const event = new Event({ warn: msg => warning = msg });
            event.subscribe(<never>it);
            event.subscribe(<never>it);
            expect(warning).include("handler is already subscribed");
        });
        it("invokes subscribed handlers in order", () => {
            let result = "";
            const event = new Event();
            event.subscribe(() => result = "foo");
            event.subscribe(() => result = "bar");
            event.broadcast();
            expect(result).toStrictEqual("bar");
        });
        it("doesn't invoke un-subscribed handler", () => {
            let result = false;
            const event = new Event();
            const handler = (v: unknown) => result = <never>v;
            event.subscribe(handler);
            event.broadcast(true);
            event.unsubscribe(handler);
            event.broadcast(false);
            expect(result).toStrictEqual(true);
        });
        it("delivers broadcast argument to the handlers", () => {
            let result = "";
            const event = new Event();
            event.subscribe(v => result = <never>v);
            event.broadcast("foo");
            expect(result).toStrictEqual("foo");
        });
        it("can broadcast multiple arguments", () => {
            let resultA, resultB;
            const event = new Event();
            event.subscribe(function (a, b) {
                resultA = a;
                resultB = b;
            });
            event.broadcast(["foo", "bar", undefined, null], "nya");
            expect(resultA).toStrictEqual(["foo", "bar", undefined, null]);
            expect(resultB).toStrictEqual("nya");
        });
        it("doesnt add same handlers multiple times", () => {
            let result = 0;
            const event = new Event({ warn: () => {} });
            const incrementer = () => result++;
            for (let i = 0; i < 10; i++)
                event.subscribe(incrementer);
            event.broadcast();
            expect(result).toStrictEqual(1);
        });
        it("can un/subscribe by id", () => {
            let result = 0;
            const event = new Event();
            const incrementer = () => result++;
            for (let i = 0; i < 10; i++)
                event.subscribeById(i.toString(), incrementer);
            event.unsubscribeById("0");
            event.broadcast();
            expect(result).toStrictEqual(9);
        });
        it("returns undefined last args until no broadcasts performed", () => {
            expect(new Event().last).toBeUndefined();
        });
        it("returns args of the last broadcasts", () => {
            const event = new Event();
            event.broadcast("foo");
            event.broadcast("bar");
            expect(event.last).toStrictEqual(["bar"]);
        });
    });
    describe("cancellation token", () => {
        it("can interop with exported cancellation token", () => {
            let cancelled = false;
            const ct = BCL.exportCancellationToken();
            ct.onCancellationRequested.subscribe(() => cancelled = true);
            expect(ct.isCancellationRequested).toStrictEqual(false);
            BCL.cancelExportedCancellationToken();
            expect(ct.isCancellationRequested).toStrictEqual(true);
            expect(cancelled).toStrictEqual(true);
            BCL.cancelExportedCancellationToken();
            BCL.cancelExportedCancellationToken();
            expect(ct.isCancellationRequested).toStrictEqual(true);
            const source = new CancellationToken();
            const echoed = BCL.echoCancellationTokenExport(source);
            expect(echoed).toBe(source);
            expect(BCL.echoCancellationTokenExport(echoed)).toBe(source);
            expect(echoed.isCancellationRequested).toBeFalsy();
            source.cancel();
            expect(echoed.isCancellationRequested).toBeTruthy();
        });
        it("can interop with imported cancellation token", () => {
            let ct = new CancellationToken();
            BCL.importCancellationToken = () => (ct = new CancellationToken());
            BCL.cancelImportedCancellationToken = () => ct.cancel();
            BCL.echoCancellationTokenImport = ct => ct;
            BCL.testCancellationTokenImport();
        });
    });
    describe("collection", () => {
        it("can use collection", () => {
            const cl = new Collection<string>();
            expect(cl.count).toStrictEqual(0);
            cl.add("a");
            cl.add("b");
            expect(cl.count).toStrictEqual(2);
            expect(cl.contains("a")).toStrictEqual(true);
            expect(cl.contains("z")).toStrictEqual(false);
            expect(cl.copy()).toStrictEqual(["a", "b"]);
            expect([...cl]).toStrictEqual(["a", "b"]);
            expect(cl.remove("a")).toStrictEqual(true);
            expect(cl.remove("z")).toStrictEqual(false);
            expect([...cl]).toStrictEqual(["b"]);
            cl.clear();
            expect(cl.count).toStrictEqual(0);
            expect([...new Collection(["x", "y"])]).toStrictEqual(["x", "y"]);
        });
        it("can interop with exported collection", () => {
            const cl = BCL.exportCollection(["a", "b"]);
            expect(cl.count).toStrictEqual(2);
            expect(cl.copy()).toStrictEqual(["a", "b"]);
            expect(cl.contains("a")).toStrictEqual(true);
            expect(cl.contains("z")).toStrictEqual(false);
            cl.add("c");
            let concat = "";
            for (const item of cl) concat += item;
            expect(concat).toStrictEqual("abc");
            expect(cl.remove("a")).toStrictEqual(true);
            expect(cl.remove("z")).toStrictEqual(false);
            expect(cl.copy()).toStrictEqual(["b", "c"]);
            cl.clear();
            expect(cl.count).toStrictEqual(0);
            cl.add("d");
            expect(cl.copy()).toStrictEqual(["d"]);
            const source = new Collection(["foo", "bar"]);
            const echoed = BCL.echoCollectionExport(source);
            expect(echoed).toBe(source);
            expect(BCL.echoCollectionExport(echoed)).toBe(source);
            source.clear();
            expect(echoed.count).toStrictEqual(0);
        });
        it("can interop with imported collection", () => {
            BCL.importCollection = items => new Collection(items);
            BCL.echoCollectionImport = cl => cl;
            BCL.testCollectionImport();
        });
    });
    describe("list", () => {
        it("can use list", () => {
            const list = new List<string>();
            expect(list.count).toStrictEqual(0);
            list.add("a");
            list.add("b");
            expect(list.getAt(0)).toStrictEqual("a");
            list.setAt(0, "z");
            expect(list.getAt(0)).toStrictEqual("z");
            expect(list.indexOf("z")).toStrictEqual(0);
            expect(list.indexOf("missing")).toStrictEqual(-1);
            list.insert(1, "m");
            expect(list.copy()).toStrictEqual(["z", "m", "b"]);
            list.removeAt(1);
            expect(list.copy()).toStrictEqual(["z", "b"]);
            expect([...list]).toStrictEqual(["z", "b"]);
        });
        it("can interop with exported list", () => {
            const list = BCL.exportList(["a", "b"]);
            expect(list.count).toStrictEqual(2);
            expect(list.getAt(0)).toStrictEqual("a");
            expect(list.getAt(1)).toStrictEqual("b");
            list.setAt(0, "x");
            expect(list.getAt(0)).toStrictEqual("x");
            list.setAt(0, "a");
            expect(list.indexOf("a")).toStrictEqual(0);
            expect(list.indexOf("b")).toStrictEqual(1);
            expect(list.copy()).toStrictEqual(["a", "b"]);
            expect(list.contains("a")).toStrictEqual(true);
            expect(list.contains("z")).toStrictEqual(false);
            list.add("c");
            let concat = "";
            for (const item of list) concat += item;
            expect(concat).toStrictEqual("abc");
            expect(list.remove("a")).toStrictEqual(true);
            expect(list.remove("z")).toStrictEqual(false);
            list.insert(1, "z");
            expect(list.copy()).toStrictEqual(["b", "z", "c"]);
            list.removeAt(1);
            expect(list.copy()).toStrictEqual(["b", "c"]);
            list.clear();
            expect(list.count).toStrictEqual(0);
            list.add("d");
            expect(list.copy()).toStrictEqual(["d"]);
            const source = new List(["foo", "bar"]);
            const echoed = BCL.echoListExport(source);
            expect(echoed).toBe(source);
            expect(BCL.echoListExport(echoed)).toBe(source);
            source.clear();
            expect(echoed.count).toStrictEqual(0);
        });
        it("can interop with imported list", () => {
            BCL.importList = items => new List(items);
            BCL.echoListImport = list => list;
            BCL.testListImport();
        });
    });
    describe("dictionary", () => {
        it("can use dictionary", () => {
            const dic = new Dictionary<string, string>();
            expect(dic.count).toStrictEqual(0);
            dic.add("a", "A");
            dic.setAt("b", "B");
            expect(dic.getAt("a")).toStrictEqual("A");
            expect(dic.containsKey("a")).toStrictEqual(true);
            expect(dic.containsKey("z")).toStrictEqual(false);
            expect(dic.getKeys()).toStrictEqual(["a", "b"]);
            expect(dic.getValues()).toStrictEqual(["A", "B"]);
            expect([...dic]).toStrictEqual([["a", "A"], ["b", "B"]]);
            expect(dic.remove("a")).toStrictEqual(true);
            expect(dic.remove("z")).toStrictEqual(false);
            dic.clear();
            expect(dic.count).toStrictEqual(0);
            expect([...new Dictionary([["x", "1"]])]).toStrictEqual([["x", "1"]]);
        });
        it("can interop with exported dictionary", () => {
            const dic = BCL.exportDictionary(new Map([["a", "A"], ["b", "B"]]));
            expect(dic.count).toStrictEqual(2);
            expect(dic.getAt("a")).toStrictEqual("A");
            expect(dic.getAt("b")).toStrictEqual("B");
            expect(dic.containsKey("a")).toStrictEqual(true);
            expect(dic.containsKey("z")).toStrictEqual(false);
            expect(dic.getKeys()).toStrictEqual(["a", "b"]);
            expect(dic.getValues()).toStrictEqual(["A", "B"]);
            dic.add("c", "C");
            dic.setAt("c", "CC");
            const kv: [string, string][] = [];
            for (const entry of dic) kv.push(entry);
            expect(kv).toStrictEqual([["a", "A"], ["b", "B"], ["c", "CC"]]);
            expect(dic.remove("a")).toStrictEqual(true);
            expect(dic.remove("z")).toStrictEqual(false);
            expect(dic.getKeys()).toStrictEqual(["b", "c"]);
            dic.clear();
            expect(dic.count).toStrictEqual(0);
            dic.add("d", "D");
            expect(dic.getValues()).toStrictEqual(["D"]);
            const source = new Dictionary([["foo", "1"], ["bar", "2"]]);
            const echoed = BCL.echoDictionaryExport(source);
            expect(echoed).toBe(source);
            expect(BCL.echoDictionaryExport(echoed)).toBe(source);
            source.clear();
            expect(echoed.count).toStrictEqual(0);
        });
        it("can interop with imported dictionary", () => {
            BCL.importDictionary = kv => new Dictionary<string, string>(kv);
            BCL.echoDictionaryImport = dic => dic;
            BCL.testDictionaryImport();
        });
    });
    describe("custom specializations", () => {
        const comparer = { compare: (x: string, y: string) => x < y ? -1 : x > y ? 1 : 0 };
        it("can interop with exported comparer", () => {
            const cmp = BCL.exportComparer();
            expect(cmp.compare("a", "b")).toBeLessThan(0);
            expect(cmp.compare("b", "a")).toBeGreaterThan(0);
            expect(cmp.compare("a", "a")).toStrictEqual(0);
            const echoed = BCL.echoComparerExport(comparer);
            expect(echoed).toBe(comparer);
            expect(BCL.echoComparerExport(echoed)).toBe(comparer);
        });
        it("can interop with imported comparer", () => {
            BCL.importComparer = () => comparer;
            BCL.echoComparerImport = cmp => cmp;
            BCL.testComparerImport();
        });
    });
});
