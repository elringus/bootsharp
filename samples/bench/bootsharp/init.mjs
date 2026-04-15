import bootsharp, { Export, Import } from "./bin/bootsharp/index.mjs";
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    Import.getNumber = getNumber;
    Import.getStruct = getStruct;

    await bootsharp.boot();

    return { ...Export };
}
