import fs from "fs";
import "./wasm_exec.js";
import { getNumber, getStruct } from "../fixtures.mjs";

/** @returns {Promise<import("../bench.mjs").Exports>} */
export async function init() {
    global.getNumber = getNumber;
    global.getStruct = () => JSON.stringify(getStruct());

    const bin = await WebAssembly.compile(fs.readFileSync("./go/main.wasm"));
    const go = new Go();
    const wasm = await WebAssembly.instantiate(bin, go.importObject);
    go.run(wasm);

    return {
        echoNumber: global.echoNumber,
        echoStruct: global.echoStruct,
        fi: global.fi
    };
}
