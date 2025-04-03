import { init as initBootsharp } from "./bootsharp/init.mjs";
import { init as initDotNet } from "./dotnet/init.mjs";
import { init as initDotNetLLVM } from "./dotnet-llvm/init.mjs";
import { init as initGo } from "./go/init.mjs";
import { init as initRust } from "./rust/init.mjs";
import * as fixtures from "./fixtures.mjs";

/**
 * @typedef {Object} Exports
 * @property {() => number} echoNumber
 * @property {() => Data} echoStruct
 * @property {(n: number) => number} fi
 */

const lang = process.argv[2];
const baseline = new Map;

if (!lang || lang.toLowerCase() === "rust")
    await run("Rust", await initRust());
if (!lang || lang.toLowerCase() === "llvm")
    await run(".NET LLVM", await initDotNetLLVM());
if (!lang || lang.toLowerCase() === "net")
    await run(".NET AOT", await initDotNet());
if (!lang || lang.toLowerCase() === "boot")
    await run("Bootsharp", await initBootsharp());
if (!lang || lang.toLowerCase() === "go")
    await run("Go", await initGo());

/** @param {string} lang
 *  @param {Exports} exports */
async function run(lang, exports) {
    console.log(`\n\nBenching ${lang}...\n`);
    await new Promise(r => setTimeout(r, 100));
    bench("Echo number", exports.echoNumber, 100, 3, 1000, fixtures.getNumber());
    bench("Echo struct", exports.echoStruct, 100, 3, 100, fixtures.getStruct());
    bench("Fibonacci", () => exports.fi(33), 100, 3, 1);
}

function bench(name, fn, iters, warms, loops, expected = undefined) {
    if (expected) {
        expected = JSON.stringify(expected);
        const actual = JSON.stringify(fn());
        if (actual !== expected) {
            console.error(`Wrong result of '${name}'. Expected: ${expected} Actual: ${actual}`);
            return;
        }
    }

    const results = [];
    warms *= -1;
    for (let i = warms; i < iters; i++) {
        const start = performance.now();
        for (let l = 0; l < loops; l++) fn();
        if (i >= 0) results.push(performance.now() - start);
    }
    let media = median(results);

    if (baseline.has(name)) {
        console.log(`${name}: ${(media / baseline.get(name)).toFixed(1)}`);
    } else {
        baseline.set(name, media);
        console.log(`${name}: ${media.toFixed(3)} ms`);
    }
}

function median(numbers) {
    const sorted = [...numbers].sort((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    if (sorted.length % 2 === 1) return sorted[middle];
    return (sorted[middle - 1] + sorted[middle]) / 2;
}
