import bootsharp, { Export, Import } from "./bin/bootsharp/index.mjs";
import { getNumber, getStruct } from "../fixtures.mjs";
import fs from "fs/promises";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    Import.getNumber = getNumber;
    Import.getStruct = getStruct;

    const content = await fs.readFile("./bootsharp/bin/bootsharp/bin/dotnet.native.wasm");
    await bootsharp.boot({
        root: "./bin",
        resources: {
            wasm: { name: "dotnet.native.wasm", content },
            assemblies: [],
            entryAssemblyName: "Boot.dll"
        }
    });

    return { ...Export };
}
