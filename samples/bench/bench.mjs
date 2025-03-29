import { Bench, hrtimeNow } from "tinybench";
import { init as initBootsharp } from "./bootsharp/init.mjs";
import { init as initDotNet } from "./dotnet/init.mjs";
import { init as initGo } from "./go/init.mjs";
import { init as initRust } from "./rust/init.mjs";

/**
 * @typedef {Object} Exports
 * @property {() => number} echoNumber
 * @property {() => Data} echoStruct
 * @property {(n: number) => number} fi
 */

await run("Rust", await initRust());
await run("DotNet", await initDotNet());
await run("Bootsharp", await initBootsharp());
await run("Go", await initGo());

/** @param {string} lang */
/** @param {Exports} exports */
async function run(lang, exports) {
    console.log(`Running ${lang}...`);
    const bench = new Bench({ hrtimeNow });
    bench.add("Echo Number", exports.echoNumber);
    bench.add("Echo Struct", exports.echoStruct);
    bench.add("Fibonacci", () => exports.fi(25));
    await bench.run();
    console.table(bench.table())
}
