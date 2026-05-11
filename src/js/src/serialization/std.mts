import { Binary, binary } from "./serializer.mjs";
import { Reader } from "./reader.mjs";
import { Writer } from "./writer.mjs";

const dotnetEpochTicks = 621355968000000000n;

export const std = {
    Boolean: binary(
        (writer, value: boolean) => writer.writeBool(value),
        reader => reader.readBool()),

    Byte: binary(
        (writer, value: number) => writer.writeByte(value),
        reader => reader.readByte(), Uint8Array),

    SByte: binary(
        (writer, value: number) => writer.writeSByte(value),
        reader => reader.readSByte(), Int8Array),

    Int16: binary(
        (writer, value: number) => writer.writeInt16(value),
        reader => reader.readInt16(), Int16Array),

    UInt16: binary(
        (writer, value: number) => writer.writeUInt16(value),
        reader => reader.readUInt16(), Uint16Array),

    Int32: binary(
        (writer, value: number) => writer.writeInt32(value),
        reader => reader.readInt32(), Int32Array),

    UInt32: binary(
        (writer, value: number) => writer.writeUInt32(value),
        reader => reader.readUInt32(), Uint32Array),

    Int64: binary(
        (writer, value: bigint) => writer.writeInt64(value),
        reader => reader.readInt64(), BigInt64Array),

    UInt64: binary(
        (writer, value: number) => writer.writeUInt64(value),
        reader => Number(reader.readUInt64())),

    IntPtr: binary(
        (writer, value: number) => writer.writeInt64(BigInt(value)),
        reader => Number(reader.readInt64())),

    Single: binary(
        (writer, value: number) => writer.writeSingle(value),
        reader => reader.readSingle(), Float32Array),

    Double: binary(
        (writer, value: number) => writer.writeDouble(value),
        reader => reader.readDouble(), Float64Array),

    Decimal: binary(
        (writer, value: number) => writer.writeDouble(value),
        reader => reader.readDouble()),

    Char: binary(
        (writer, value: string) => writer.writeUInt16(((String(value ?? ""))[0] ?? "\0").charCodeAt(0)),
        reader => String.fromCharCode(reader.readUInt16())),

    String: binary(
        (writer, value: string | null | undefined) => writer.writeString(value),
        reader => reader.readString()),

    DateTime: binary(
        (writer, value: Date) => writer.writeInt64((BigInt(value.getTime()) * 10000n) + dotnetEpochTicks),
        reader => new Date(Number((reader.readInt64() - dotnetEpochTicks) / 10000n))),

    DateTimeOffset: binary(
        (writer, value: Date) => writer.writeInt64((BigInt(value.getTime()) * 10000n) + dotnetEpochTicks),
        reader => new Date(Number((reader.readInt64() - dotnetEpochTicks) / 10000n))),

    Nullable: <T, >(inner: Binary<T>): Binary<T | null | undefined> => binary(
        (writer, value) => writeNullable(writer, value, inner),
        reader => readNullable(reader, inner)),

    Array: <T, >(element: Binary<T>): Binary<ArrayLike<T> | null | undefined> => binary(
        (writer, value) => writeArray(writer, value, element),
        reader => readArray(reader, element)),

    List: <T, >(element: Binary<T>): Binary<ArrayLike<T> | null | undefined> => binary(
        (writer, value) => writeList(writer, value, element),
        reader => readList(reader, element)),

    Dictionary: <TKey, TValue>(key: Binary<TKey>, value: Binary<TValue>): Binary<Map<TKey, TValue> | null | undefined> => binary(
        (writer, map) => writeDictionary(writer, map, key, value),
        reader => readDictionary(reader, key, value))
};

function writeNullable<T>(writer: Writer, value: T | null | undefined, inner: Binary<T>): void {
    writer.writeBool(value != null);
    if (value != null) inner.write(writer, value);
}

function readNullable<T>(reader: Reader, inner: Binary<T>): T | null {
    return reader.readBool() ? inner.read(reader) : null;
}

function writeArray<T>(writer: Writer, value: ArrayLike<T> | null | undefined, element: Binary<T>): void {
    if (value == null) {
        writer.writeMeta(-1);
        return;
    }
    writer.writeMeta(value.length);
    if (element.arrayCtor && value instanceof element.arrayCtor)
        writer.writeBytes(new Uint8Array(value.buffer, value.byteOffset, value.byteLength));
    else for (let i = 0; i < value.length; i++)
        element.write(writer, value[i]!);
}

function readArray<T>(reader: Reader, element: Binary<T>): ArrayLike<T> | null {
    const count = reader.readMeta();
    if (count < 0) return null;
    if (element.arrayCtor) {
        const result = new element.arrayCtor(count);
        reader.readBytes(new Uint8Array(result.buffer, result.byteOffset, result.byteLength));
        return result as unknown as ArrayLike<T>;
    }
    const result = new Array(count);
    for (let i = 0; i < count; i++)
        result[i] = element.read(reader)!;
    return result;
}

function writeList<T>(writer: Writer, value: ArrayLike<T> | null | undefined, element: Binary<T>): void {
    if (value == null) {
        writer.writeMeta(-1);
        return;
    }
    writer.writeMeta(value.length);
    for (let i = 0; i < value.length; i++)
        element.write(writer, value[i]!);
}

function readList<T>(reader: Reader, element: Binary<T>): T[] | null {
    const count = reader.readMeta();
    if (count < 0) return null;
    const result = new Array(count);
    for (let i = 0; i < count; i++)
        result[i] = element.read(reader)!;
    return result;
}

function writeDictionary<TKey, TValue>(writer: Writer, map: Map<TKey, TValue> | null | undefined, key: Binary<TKey>, value: Binary<TValue>): void {
    if (map == null) {
        writer.writeMeta(-1);
        return;
    }
    writer.writeMeta(map.size);
    for (const pair of map) {
        key.write(writer, pair[0]);
        value.write(writer, pair[1]);
    }
}

function readDictionary<TKey, TValue>(reader: Reader, key: Binary<TKey>, value: Binary<TValue>): Map<TKey, TValue> | null {
    const count = reader.readMeta();
    if (count < 0) return null;
    const result = new Map<TKey, TValue>();
    for (let i = 0; i < count; i++)
        result.set(key.read(reader)!, value.read(reader)!);
    return result;
}
