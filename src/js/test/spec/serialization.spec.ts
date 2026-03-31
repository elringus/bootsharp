import { beforeAll, describe, expect, it } from "vitest";
import { Test, bootSideload } from "../cs";

describe("serialization", () => {
    beforeAll(bootSideload);

    it("can echo primitives", () => {
        const input: Test.Primitives = {
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
        const expected: Test.Primitives = {
            ...input,
            emptyChar: "\0",
            missingChar: "\0"
        };
        expect(Test.Serialization.echoPrimitives([input, null])).toStrictEqual([expected, null]);
        expect(Test.Serialization.echoPrimitives(undefined)).toBeNull();
    });

    it("can echo unions", () => {
        const a: Test.Union = { shared: "A", a: { string: "*", map: new Map([["a", null], ["b", 7]]) } };
        const b: Test.Union = { shared: "B", b: { ints: [], strings: ["foo", "bar"], times: [new Date()] } };
        expect(Test.Serialization.echoUnions([a, b, null])).toStrictEqual([a, b, null]);
        expect(Test.Serialization.echoUnions(undefined)).toBeNull();
    });

    it("can echo vehicles", async () => {
        expect(Test.Types.Registry.echoVehicles([{ id: "foo", maxSpeed: 1 }, null]))
            .toStrictEqual([{ id: "foo", maxSpeed: 1 }, null]);
        expect(Test.Types.Registry.echoRegistry({
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 2 }, null],
            tracked: [{ id: "bar", maxSpeed: 2, trackType: Test.Types.TrackType.Chain }, null]
        })).toStrictEqual({
            wheeled: [{ id: "foo", maxSpeed: 1, wheelCount: 2 }, null],
            tracked: [{ id: "bar", maxSpeed: 2, trackType: Test.Types.TrackType.Chain }, null]
        });
        Test.Types.RegistryProvider.getRegistries = () => [];
        await expect(Test.Types.Registry.concatRegistriesAsync([null])).resolves.toStrictEqual([null]);
    });

    it("can echo arrays", () => {
        expect(Test.Serialization.echoBytes(new Uint8Array([1, 2, 3]))).toStrictEqual(new Uint8Array([1, 2, 3]));
        expect(Test.Serialization.echoIntArray(new Int32Array([-1, 0, 1]))).toStrictEqual(new Int32Array([-1, 0, 1]));
        expect(Test.Serialization.echoDoubleArray(new Float64Array([0.5, -1.9]))).toStrictEqual(new Float64Array([0.5, -1.9]));
        expect(Test.Serialization.echoStringArray(["a", null, "", "b"])).toStrictEqual(["a", null, "", "b"]);
        expect(Test.Serialization.echoNullableIntArray([1, null, -1])).toStrictEqual([1, null, -1]);
        expect(Test.Serialization.echoNestedIntArray([new Int32Array([-1, 2]), null, new Int32Array()])).toStrictEqual([new Int32Array([-1, 2]), null, new Int32Array()]);
        expect(Test.Serialization.echoBytes(undefined)).toBeNull();
        expect(Test.Serialization.echoIntArray(undefined)).toBeNull();
        expect(Test.Serialization.echoDoubleArray(undefined)).toBeNull();
        expect(Test.Serialization.echoStringArray(undefined)).toBeNull();
        expect(Test.Serialization.echoNullableIntArray(undefined)).toBeNull();
        expect(Test.Serialization.echoNestedIntArray(undefined)).toBeNull();
    });

    it("can echo lists", () => {
        expect(Test.Serialization.echoIntList([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Test.Serialization.echoStringList(["a", null, "", "b"])).toStrictEqual(["a", null, "", "b"]);
        expect(Test.Serialization.echoNestedIntList([[1, 2], null, []])).toStrictEqual([[1, 2], null, []]);
        expect(Test.Serialization.echoListInterface([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Test.Serialization.echoReadOnlyList([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Test.Serialization.echoCollection([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Test.Serialization.echoReadOnlyCollection([1, 2, 3])).toStrictEqual([1, 2, 3]);
        expect(Test.Serialization.echoIntList(undefined)).toBeNull();
        expect(Test.Serialization.echoStringList(undefined)).toBeNull();
        expect(Test.Serialization.echoNestedIntList(undefined)).toBeNull();
    });

    it("can echo dictionaries", () => {
        expect(Test.Serialization.echoDictionary(new Map([["1", "a"], ["2", null], ["3", ""]])))
            .toStrictEqual(new Map([["1", "a"], ["2", null], ["3", ""]]));
        expect(Test.Serialization.echoNestedDictionary([new Map([["1", "a"]]), null, new Map([["2", null]])]))
            .toStrictEqual([new Map([["1", "a"]]), null, new Map([["2", null]])]);
        expect(Test.Serialization.echoDictionaryInterface(new Map([[1, 2], [3, 4], [5, 0]])))
            .toStrictEqual(new Map([[1, 2], [3, 4], [5, 0]]));
        expect(Test.Serialization.echoReadOnlyDictionary(new Map([[1, 2], [3, 4], [5, 0]])))
            .toStrictEqual(new Map([[1, 2], [3, 4], [5, 0]]));
        expect(Test.Serialization.echoDictionary(undefined)).toBeNull();
        expect(Test.Serialization.echoNestedDictionary(undefined)).toBeNull();
    });
});
