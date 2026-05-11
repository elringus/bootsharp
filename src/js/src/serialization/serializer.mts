import { Reader } from "./reader.mjs";
import { Writer } from "./writer.mjs";

export type Binary<T> = {
    write: Write<T>;
    read: Read<T>;
    arrayCtor?: TypedArrayCtor;
};

type Write<T> = (writer: Writer, value: T) => void;
type Read<T> = (reader: Reader) => T | null;
type TypedArrayCtor = new(length: number) => TypedArray;
type TypedArray = Uint8Array | Int8Array | Uint16Array | Int16Array | Uint32Array | Int32Array | BigInt64Array | Float32Array | Float64Array;

export function binary<T>(write: Write<T>, read: Read<T>, arrayCtor?: TypedArrayCtor): Binary<T> {
    return { write, read, arrayCtor };
}

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
