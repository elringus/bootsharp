import { beforeAll, describe, expect, it, vi } from "vitest";
import { bootRuntime } from "../cs";
import type { Primitives, Union } from "../cs/Test/bin/bootsharp/generated/modules/test.g.mjs";
import { Serialization, ItemA } from "../cs/Test/bin/bootsharp/generated/modules/test.g.mjs";
import { Registries, IRegistryProvider, Modules, TrackType, Record, IBidirectional } from "../cs/Test/bin/bootsharp/generated/modules/test/library.g.mjs";

describe("serialization", () => {
    beforeAll(bootRuntime);
    it("can echo primitives", () => {
        const input: Primitives = {
            boolean: true,
            byte: 7,
            sByte: -7,
            positiveSByte: 7,
            int16: -1234,
            uInt16: 1234,
            int32: -1234567,
            uInt32: 1234567,
            int64: -1234567890123456789n,
            uInt64: 1234567890123,
            intPtr: 0x12345678,
            single: Math.fround(123.25),
            double: 456.5,
            decimal: 123.5,
            char: "X",
            emptyChar: "",
            missingChar: <never>undefined,
            string: "bootsharp",
            emptyString: "",
            largeString: "x".repeat(5_000_000),
            dateTime: new Date(Date.UTC(2024, 0, 2, 3, 4, 5)),
            dateTimeOffset: new Date(Date.UTC(2024, 0, 2, 0, 4, 5)),
            nullableInt: 42,
            missingInt: -1
        };
        const expected: Primitives = {
            ...input,
            emptyChar: "\0",
            missingChar: "\0"
        };
        expect(Serialization.echoPrimitives([input, null])).toStrictEqual([expected, null]);
        expect(Serialization.echoPrimitives(undefined)).toBeNull();
    });
    it("can echo primitives with all nullable fields omitted", () => {
        const input: Primitives = {
            boolean: false, byte: 0, sByte: 0, positiveSByte: 0,
            int16: 0, uInt16: 0, int32: 0, uInt32: 0,
            int64: 0n, uInt64: 0, intPtr: 0,
            single: 0, double: 0, decimal: 0,
            char: "\0", emptyChar: "\0", missingChar: "\0",
            dateTime: new Date(0), dateTimeOffset: new Date(0)
        };
        expect(Serialization.echoPrimitives([input])).toStrictEqual([input]);
    });
    it("can echo unions", () => {
        const a: Union = { shared: "A", a: { string: "*", map: new Map([["a", null], ["b", 7]]) } };
        const b: Union = { shared: "B", b: { ints: [], strings: ["foo", "bar"], times: [new Date()] } };
        expect(Serialization.echoUnions([a, b, null])).toStrictEqual([a, b, null]);
        expect(Serialization.echoUnions(undefined)).toBeNull();
    });
    it("can echo unions with all nullable fields omitted", () => {
        const a: Union = { shared: "A", a: {} };
        const b: Union = { shared: "B", b: { strings: ["x"], times: [new Date()] } };
        expect(Serialization.echoUnions([a, b])).toStrictEqual([a, b]);
    });
    it("can echo vehicles", async () => {
        expect(Registries.echoRecords([{ id: "foo" }, null]))
            .toStrictEqual([{ id: "foo" }, null]);
        expect(Registries.echoVehicles([{ id: "foo", maxSpeed: 1 }, null]))
            .toStrictEqual([{ id: "foo", maxSpeed: 1 }, null]);
        expect(Registries.echoRegistry({
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 2 }, null],
            tracked: [{ id: "bar", maxSpeed: 2, trackType: TrackType.Chain }, null]
        })).toStrictEqual({
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 2 }, null],
            tracked: [{ id: "bar", maxSpeed: 2, trackType: TrackType.Chain }, null]
        });
        IRegistryProvider.getRegistries = () => [];
        await expect(Registries.concatRegistriesAsync([null])).resolves.toStrictEqual([null]);
    });
    it("can echo arrays", () => {
        expect(Serialization.echoBytes(new Uint8Array([1, 2, 3]))).toStrictEqual(new Uint8Array([1, 2, 3]));
        expect(Serialization.echoIntArray(new Int32Array([-1, 0, 1]))).toStrictEqual(new Int32Array([-1, 0, 1]));
        expect(Serialization.echoDoubleArray(new Float64Array([0.5, -1.9]))).toStrictEqual(new Float64Array([0.5, -1.9]));
        expect(Serialization.echoStringArray(["a", null, "", "b"])).toStrictEqual(["a", null, "", "b"]);
        expect(Serialization.echoNullableIntArray([1, null, -1])).toStrictEqual([1, null, -1]);
        expect(Serialization.echoNestedIntArray([new Int32Array([-1, 2]), null, new Int32Array()])).toStrictEqual([new Int32Array([-1, 2]), null, new Int32Array()]);
        expect(Serialization.echoBytes(undefined)).toBeNull();
        expect(Serialization.echoIntArray(undefined)).toBeNull();
        expect(Serialization.echoDoubleArray(undefined)).toBeNull();
        expect(Serialization.echoStringArray(undefined)).toBeNull();
        expect(Serialization.echoNullableIntArray(undefined)).toBeNull();
        expect(Serialization.echoNestedIntArray(undefined)).toBeNull();
    });
    it("can echo lists", () => {
        expect(Serialization.echoIntList([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Serialization.echoStringList(["a", null, "", "b"])).toStrictEqual(["a", null, "", "b"]);
        expect(Serialization.echoNestedIntList([[1, 2], null, []])).toStrictEqual([[1, 2], null, []]);
        expect(Serialization.echoListInterface([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Serialization.echoReadOnlyList([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Serialization.echoCollection([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Serialization.echoReadOnlyCollection([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Serialization.echoIntList(undefined)).toBeNull();
        expect(Serialization.echoStringList(undefined)).toBeNull();
        expect(Serialization.echoNestedIntList(undefined)).toBeNull();
    });
    it("can echo dictionaries", () => {
        expect(Serialization.echoDictionary(new Map([["1", "a"], ["2", null], ["3", ""]])))
            .toStrictEqual(new Map([["1", "a"], ["2", null], ["3", ""]]));
        expect(Serialization.echoNestedDictionary([new Map([["1", "a"]]), null, new Map([["2", null]])]))
            .toStrictEqual([new Map([["1", "a"]]), null, new Map([["2", null]])]);
        expect(Serialization.echoDictionaryInterface(new Map([[1, 2], [3, 4], [5, 0]])))
            .toStrictEqual(new Map([[1, 2], [3, 4], [5, 0]]));
        expect(Serialization.echoReadOnlyDictionary(new Map([[1, 2], [3, 4], [5, 0]])))
            .toStrictEqual(new Map([[1, 2], [3, 4], [5, 0]]));
        expect(Serialization.echoDictionary(undefined)).toBeNull();
        expect(Serialization.echoNestedDictionary(undefined)).toBeNull();
    });
    it("imported instances survive serialization", () => {
        const bi = Modules.exportBi();
        const aHandler = vi.fn();
        const biHandler = vi.fn();
        const changed = (item?: ItemA, record?: Record) => aHandler(item, record);
        const biChanged = (b?: IBidirectional, record?: Record) => biHandler(b, record);
        const getChanged = (b: IBidirectional) => b === bi ? biChanged : <never>null;
        Serialization.importedInstancesSurviveSerialization(
            { shared: "", a: { bi, changed }, b: { strings: [], times: [], getChanged } }, bi
        );
        expect(aHandler).toHaveBeenCalledWith(expect.objectContaining({ bi, changed }), { id: "a-rec" });
        expect(biHandler).toHaveBeenCalledWith(bi, { id: "bi-rec" });
    });
    it("exported instances survive serialization", () => {
        const bi = Modules.exportBi();
        const handler = vi.fn();
        const changed = (item?: ItemA, record?: Record) => handler(item, record);
        const getChanged = (b: IBidirectional) => b === bi ? changed : <never>null;
        const echoed = Serialization.echoUnions([{
            shared: "", a: { bi, changed }, b: { strings: [], times: [], getChanged }
        }])![0]!;
        expect(echoed.a!.bi).toStrictEqual(bi);
        expect(echoed.a!.changed).toStrictEqual(changed);
        expect(echoed.b!.getChanged).toStrictEqual(getChanged);
        expect(echoed.b!.getChanged!(bi)).toStrictEqual(changed);
        echoed.b!.getChanged!(bi)!(bi, undefined);
        expect(handler).toHaveBeenCalledWith(bi, undefined);
    });
});
