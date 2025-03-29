import { echoNumber, echoStruct, fi } from './pkg/rust_wasm.js';
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    global.getNumber = getNumber;
    global.getStruct = () => JSON.stringify(getStruct());
    return { echoNumber, echoStruct, fi };
}
