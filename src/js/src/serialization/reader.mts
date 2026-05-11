import { getHeap, free } from "../runtime.mjs";

const utf16Decoder = new TextDecoder("utf-16le");

export class Reader {
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
