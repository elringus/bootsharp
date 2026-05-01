import bootsharp, { Exported, Imported } from "./bin/bootsharp/index.mjs";
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    Imported.getNumber = getNumber;
    Imported.getStruct = getStruct;

    await bootsharp.boot();

    return { ...Exported };
}
