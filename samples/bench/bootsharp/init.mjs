import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import bootsharp, { IExported, IImported } from "./bin/bootsharp/index.mjs";
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    IImported.getNumber = getNumber;
    IImported.getStruct = getStruct;

    const wasm = await readFile(fileURLToPath(new URL(`./bin/bootsharp/bin/${bootsharp.manifest.wasm}`, import.meta.url)));
    await bootsharp.boot({ wasm: wasm.buffer.slice(wasm.byteOffset, wasm.byteOffset + wasm.byteLength) });

    return { ...IExported };
}
