import { malloc, free, getHeap } from "./runtime";

export type Binary<T> = {
    write: Write<T>;
    read: Read<T>;
    arrayCtor?: TypedArrayCtor;
};

type Write<T> = (writer: Writer, value: T) => void;
type Read<T> = (reader: Reader) => T | null;
type TypedArray = Uint8Array | Int8Array | Uint16Array | Int16Array | Uint32Array | Int32Array | BigInt64Array | Float32Array | Float64Array;
type TypedArrayCtor = new(length: number) => TypedArray;

const utf16Decoder = new TextDecoder("utf-16le");
const dotnetEpochTicks = 621355968000000000n;

export function serialize<T>(value: T | null | undefined, type: Binary<T>): bigint {
    if (value == null) return 0n;
    const writer = new Writer();
    type.write(writer, value);
    return writer.detach();
}

export function deserialize<T>(handle: bigint | null | undefined, type: Binary<T>): T | null {
    if (handle == null || handle === 0n) return null;
    const reader = new Reader(handle);
    const result = type.read(reader);
    reader.dispose();
    return result;
}

export function binary<T>(write: Write<T>, read: Read<T>, arrayCtor?: TypedArrayCtor): Binary<T> {
    return { write, read, arrayCtor };
}

export const types = {
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

    Nullable: <T>(inner: Binary<T>): Binary<T | null | undefined> => binary(
        (writer, value) => writeNullable(writer, value, inner),
        reader => readNullable(reader, inner)),

    Array: <T>(element: Binary<T>): Binary<ArrayLike<T> | null | undefined> => binary(
        (writer, value) => writeArray(writer, value, element),
        reader => readArray(reader, element)),

    List: <T>(element: Binary<T>): Binary<ArrayLike<T> | null | undefined> => binary(
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

class Writer {
    private heap: Uint8Array;
    private ptr: number;
    private offset: number;
    private capacity: number;
    private view: DataView;

    constructor() {
        this.capacity = 256;
        this.ptr = malloc(this.capacity);
        this.offset = 0;
        this.heap = getHeap();
        this.view = new DataView(this.heap.buffer, this.heap.byteOffset);
    }

    detach(): bigint {
        const handle = BigInt(this.ptr >>> 0);
        this.ptr = 0;
        this.capacity = 0;
        this.offset = 0;
        return handle;
    }

    writeMeta(value: number): void {
        let zigzag = ((value << 1) ^ (value >> 31)) >>> 0;
        this.ensure(5);
        let position = this.ptr + this.offset;
        while (zigzag >= 0x80) {
            this.heap[position++] = (zigzag | 0x80) & 0xff;
            zigzag >>>= 7;
        }
        this.heap[position++] = zigzag;
        this.offset = position - this.ptr;
    }

    writeString(value: string | null | undefined): void {
        if (value == null) {
            this.writeMeta(-1);
            return;
        }
        const length = value.length;
        const bytes = length * 2;
        this.writeMeta(length);
        this.ensure(bytes);
        const base = this.ptr + this.offset;
        for (let i = 0, p = base; i < length; i++, p += 2)
            this.view.setUint16(p, value.charCodeAt(i), true);
        this.offset += bytes;
    }

    writeBytes(value: Uint8Array): void {
        this.ensure(value.byteLength);
        this.heap.set(value, this.ptr + this.offset);
        this.offset += value.byteLength;
    }

    writeByte(value: number): void {
        this.ensure(1);
        this.heap[this.ptr + this.offset++] = value & 0xff;
    }

    writeSByte(value: number): void {
        this.writeByte(value);
    }

    writeBool(value: boolean): void {
        this.writeByte(value ? 1 : 0);
    }

    writeUInt16(value: number): void {
        this.ensure(2);
        this.view.setUint16(this.ptr + this.offset, value, true);
        this.offset += 2;
    }

    writeInt16(value: number): void {
        this.ensure(2);
        this.view.setInt16(this.ptr + this.offset, value, true);
        this.offset += 2;
    }

    writeUInt32(value: number): void {
        this.ensure(4);
        this.view.setUint32(this.ptr + this.offset, value, true);
        this.offset += 4;
    }

    writeInt32(value: number): void {
        this.ensure(4);
        this.view.setInt32(this.ptr + this.offset, value, true);
        this.offset += 4;
    }

    writeUInt64(value: bigint | number): void {
        this.ensure(8);
        this.view.setBigUint64(this.ptr + this.offset, BigInt(value), true);
        this.offset += 8;
    }

    writeInt64(value: bigint | number): void {
        this.ensure(8);
        this.view.setBigInt64(this.ptr + this.offset, BigInt(value), true);
        this.offset += 8;
    }

    writeSingle(value: number): void {
        this.ensure(4);
        this.view.setFloat32(this.ptr + this.offset, value, true);
        this.offset += 4;
    }

    writeDouble(value: number): void {
        this.ensure(8);
        this.view.setFloat64(this.ptr + this.offset, value, true);
        this.offset += 8;
    }

    private ensure(count: number): void {
        if (this.capacity - this.offset >= count) return;
        const capacity = Math.max(this.capacity * 2, this.offset + count);
        const sourcePtr = this.ptr;
        const ptr = malloc(capacity);
        this.refreshHeapView();
        this.heap.copyWithin(ptr, sourcePtr, sourcePtr + this.offset);
        free(sourcePtr);
        this.ptr = ptr;
        this.capacity = capacity;
    }

    private refreshHeapView(): void {
        const heap = getHeap();
        if (this.heap === heap) return;
        this.heap = heap;
        this.view = new DataView(heap.buffer, heap.byteOffset);
    }
}

class Reader {
    private readonly heap: Uint8Array;
    private readonly ptr: number;
    private offset: number;
    private view: DataView;

    constructor(handle: bigint) {
        this.ptr = Number(handle & 0xffffffffn);
        this.offset = 0;
        this.heap = getHeap();
        this.view = new DataView(this.heap.buffer, this.heap.byteOffset);
    }

    dispose(): void {
        free(this.ptr);
    }

    readMeta(): number {
        let result = 0;
        let shift = 0;
        let next;
        let position = this.ptr + this.offset;
        do {
            next = this.heap[position++];
            result = (result | ((next & 0x7f) << shift)) >>> 0;
            shift += 7;
        } while ((next & 0x80) !== 0);
        this.offset = position - this.ptr;
        return (result >>> 1) ^ -(result & 1);
    }

    readString(): string | null {
        const count = this.readMeta();
        if (count < 0) return null;
        const bytes = count * 2;
        const start = this.ptr + this.offset;
        const value = utf16Decoder.decode(this.heap.subarray(start, start + bytes));
        this.offset += bytes;
        return value;
    }

    readBytes(destination: Uint8Array): void {
        destination.set(this.heap.subarray(this.ptr + this.offset, this.ptr + this.offset + destination.byteLength));
        this.offset += destination.byteLength;
    }

    readByte(): number {
        return this.heap[this.ptr + this.offset++];
    }

    readSByte(): number {
        const value = this.readByte();
        return value > 127 ? value - 256 : value;
    }

    readBool(): boolean {
        return this.readByte() !== 0;
    }

    readUInt16(): number {
        const value = this.view.getUint16(this.ptr + this.offset, true);
        this.offset += 2;
        return value;
    }

    readInt16(): number {
        const value = this.view.getInt16(this.ptr + this.offset, true);
        this.offset += 2;
        return value;
    }

    readUInt32(): number {
        const value = this.view.getUint32(this.ptr + this.offset, true);
        this.offset += 4;
        return value;
    }

    readInt32(): number {
        const value = this.view.getInt32(this.ptr + this.offset, true);
        this.offset += 4;
        return value;
    }

    readUInt64(): bigint {
        const value = this.view.getBigUint64(this.ptr + this.offset, true);
        this.offset += 8;
        return value;
    }

    readInt64(): bigint {
        const value = this.view.getBigInt64(this.ptr + this.offset, true);
        this.offset += 8;
        return value;
    }

    readSingle(): number {
        const value = this.view.getFloat32(this.ptr + this.offset, true);
        this.offset += 4;
        return value;
    }

    readDouble(): number {
        const value = this.view.getFloat64(this.ptr + this.offset, true);
        this.offset += 8;
        return value;
    }
}
