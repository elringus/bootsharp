import bootsharp, { IExported, IImported } from "./bin/bootsharp/index.mjs";
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    IImported.getNumber = getNumber;
    IImported.getStruct = getStruct;

    await bootsharp.boot();

    return { ...IExported };
}
