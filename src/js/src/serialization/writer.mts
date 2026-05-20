import { getHeap, malloc, free } from "../runtime.mjs";

export class Writer {
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
        /* v8 ignore start -- @preserve */ // Uncoverable, as WASM heap growth is not controllable.
        if (this.heap === heap) return;
        /* v8 ignore stop -- @preserve */
        this.heap = heap;
        this.view = new DataView(heap.buffer, heap.byteOffset);
    }
}
