import { getNumber, getStruct } from "../fixtures.mjs";
import fs from "fs/promises";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    const source = await fs.readFile("./zig/zig-out/bin/zig.wasm");
    const { instance: { exports } } = await WebAssembly.instantiate(source, {
        x: {
            getNumber,
            getStruct: () => encodeString(JSON.stringify(getStruct())),
        }
    });
    memory = exports.memory, cached = new Uint8Array(memory.buffer);

    return {
        echoNumber: exports.echoNumber,
        echoStruct: () => JSON.parse(decodeString(exports.echoStruct())),
        fi: exports.fi
    };
}

let memory, cached;
const encoder = new TextEncoder("utf-8");
const decoder = new TextDecoder("utf-8");
const mask = BigInt("0xFFFFFFFF");

function encodeString(str) {
    const memory = getMemoryCached();
    const { written } = encoder.encodeInto(str, memory);
    return BigInt(written) << BigInt(32) | BigInt(0);
}

function decodeString(ptrAndLen) {
    const memory = getMemoryCached();
    const ptr = Number(ptrAndLen & mask);
    const len = Number(ptrAndLen >> BigInt(32));
    const bytes = memory.subarray(ptr, ptr + len);
    return decoder.decode(bytes);
}

function getMemoryCached() {
    if (cached.buffer === memory.buffer) return cached;
    return cached = new Uint8Array(memory.buffer);
}
